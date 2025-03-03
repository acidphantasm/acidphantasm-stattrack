import type { DependencyContainer } from "tsyringe";

import type { ItemHelper } from "@spt/helpers/ItemHelper";
import type { ProfileHelper } from "@spt/helpers/ProfileHelper";
import type { Item } from "@spt/models/eft/common/tables/IItem";
import type { IPostSptLoadMod } from "@spt/models/external/IPostSptLoadMod";
import type { IPreSptLoadMod } from "@spt/models/external/IPreSptLoadMod";
import type { ILogger } from "@spt/models/spt/utils/ILogger";
import type { StaticRouterModService } from "@spt/services/mod/staticRouter/StaticRouterModService";
import type { ICloner } from "@spt/utils/cloners/ICloner";
import type { VFS } from "@spt/utils/VFS";

import fs from "node:fs";
import path from "node:path";

type Stats = Record<string, Record<string, number[]>>;

class StatTrack implements IPreSptLoadMod, IPostSptLoadMod {
    private logger: ILogger;
    private vfs: VFS;
    private profileHelper: ProfileHelper;
    private itemHelper: ItemHelper;
    private stats: Stats = null;
    private filepath: string;

    public preSptLoad(container: DependencyContainer): void {
        this.logger = container.resolve<ILogger>("PrimaryLogger");
        this.itemHelper = container.resolve<ItemHelper>("ItemHelper")
        this.vfs = container.resolve<VFS>("VFS");
        const cloner = container.resolve<ICloner>("RecursiveCloner");

        const staticRouterModService = container.resolve<StaticRouterModService>("StaticRouterModService");

        this.filepath = path.resolve(__dirname, "../stats.json");
        this.load();

        staticRouterModService.registerStaticRouter(
            "StatTrackRoutes",
            [
                {
                    url: "/stattrack/save",
                    action: async (url, info: Stats, sessionId, output) => this.saveStats(info)
                },
                {
                    url: "/stattrack/load",
                    action: async (url, info, sessionId, output) => JSON.stringify(this.stats)
                }
            ],
            "custom-static-stattrack"
        );

        // listen to ItemHelper.replaceIDs to keep in sync
        container.afterResolution(
            "ItemHelper",
            (_, itemHelper: ItemHelper) => {
                const originalReplaceIDs = itemHelper.replaceIDs;
                itemHelper.replaceIDs = (originalItems, pmcData, insuredItems, fastPanel) => {
                    const results: Item[] = originalReplaceIDs.call(
                        itemHelper,
                        originalItems,
                        pmcData,
                        insuredItems,
                        fastPanel
                    );

                    let dirty = false;
                    for (let i = 0; i < originalItems.length; i++) {
                        const oldId = originalItems[i]._id;
                        
                        for (const profileId of Object.keys(this.stats)) {
                            if (oldId in this.stats[profileId]) {
                                const newId = results[i]._id;
                                this.stats[profileId][newId] = cloner.clone(this.stats[profileId][oldId]);

                                dirty = true;
                                this.logger.info(
                                    `StatTrack: Weapon ${oldId} is now ${newId}, stats copied`);
                                delete this.stats[profileId][oldId];
                            }
                        }
                    }

                    if (dirty) {
                        this.save();
                    }

                    return results;
                };
            },
            { frequency: "Always" }
        );
    }

    public postSptLoad(container: DependencyContainer): void {
        this.profileHelper = container.resolve<ProfileHelper>("ProfileHelper");
        // this.clean();
        // Disabled

        const profilesCount = Object.keys(this.stats).length;
        if (profilesCount > 0) {
            this.logger.info(`StatTrack: ${profilesCount} profiles loaded.`);''
        }
    }

    private async saveStats(payload: Stats): Promise<string> {
        if (!payload) {
            this.logger.error("StatTrack: Bad save payload!");
            return JSON.stringify({ success: false });;
        }
        this.stats = payload
        await this.save();

        return JSON.stringify({ success: true });
    }

    private load() {
        try {
            if (this.vfs.exists(this.filepath)) {
                this.stats = JSON.parse(this.vfs.readFile(this.filepath));
            } else {
                this.stats = {};

                // Create the file with fs - vfs.writeFile pukes on windows paths if it needs to create the file
                fs.writeFileSync(this.filepath, JSON.stringify(this.stats));
            }
        } catch (error) {
            this.logger.error("StatTrack: Failed to load weapon stats! " + error);
            this.stats = {};
        }
    }

    // Remove any stats for items that no longer exist
    // Might delete unclaimed items in the mail?
    private async clean() {
        this.logger.info(`StatTrack: Cleaning profiles.`) // Debug
        const map = new Map<string, boolean>();
        for (const profileId of Object.keys(this.stats)) {
            for (const weaponId of Object.keys(this.stats[profileId])) {
                map.set(weaponId, false);
            }
            for (const profile of Object.values(this.profileHelper.getProfiles())) {
                if (profile.characters?.pmc?._id != profileId) { // If not current profile, skip profile
                    continue
                }
                const items = profile.characters?.pmc?.Inventory?.items ?? [];
                for (const item of items) {
                    if (map.has(item._id)) {
                        map.set(item._id, true);
                    }
                }
            }
            let dirtyCount = 0;
            map.forEach((found: boolean, id: string) => {
                if (!found && !this.itemHelper.isItemInDb(id)) { //  If not found within items of profile AND not an actual itemId - delete. (StatTrack tracks total weapon kills via itemId of weapon)
                    this.logger.debug(`StatTrack: Deleting ${id} weapon stat from profile ${profileId}.`) // Debug
                    delete this.stats[profileId][id];
                    dirtyCount++;
                }

            });
            if (dirtyCount > 0) {
                this.logger.info(
                    `StatTrack: Cleaned up ${dirtyCount} stats for weapons that no longer exist for profile ${profileId}.`);
                await this.save();
            }
            map.clear();
        }
    }

    private async save() {
        try {
            await this.vfs.writeFileAsync(this.filepath, JSON.stringify(this.stats, null, 2));
        } catch (error) {
            this.logger.error("StatTrack: Failed to save weapon stats! " + error);
        }
    }
}

export const mod = new StatTrack();
