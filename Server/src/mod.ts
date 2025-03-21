import { DependencyContainer } from "tsyringe";

import { IPostDBLoadMod } from "@spt/models/external/IPostDBLoadMod";
import { IPreSptLoadMod } from "@spt/models/external/IPreSptLoadMod";
import { FileSystemSync } from "@spt/utils/FileSystemSync";
import { ILogger } from "@spt/models/spt/utils/ILogger";
import { ItemHelper } from "@spt/helpers/ItemHelper";
import { LogTextColor } from "@spt/models/spt/logging/LogTextColor";
import { StaticRouterModService } from "@spt/services/mod/staticRouter/StaticRouterModService";
import { ICloner } from "@spt/utils/cloners/ICloner";

import fs from "node:fs";
import path from "node:path";
import { ProfileHelper } from "@spt/helpers/ProfileHelper";
import { IItem } from "@spt/models/eft/common/tables/IItem";

class StatTrack implements IPreSptLoadMod
{
    private logger: ILogger;
    private fileSystem: FileSystemSync;
    private profileHelper: ProfileHelper;
    private weaponStats: WeaponStats = null;

    public preSptLoad(container: DependencyContainer): void
    {
        this.logger = container.resolve<ILogger>("PrimaryLogger");
        this.fileSystem = container.resolve<FileSystemSync>("FileSystemSync");
        const staticRouterModService = container.resolve<StaticRouterModService>("StaticRouterModService");
        const cloner = container.resolve<ICloner>("RecursiveCloner");

        this.load();

        staticRouterModService.registerStaticRouter(
            "StatTrack-GameStartRouter",
            [
                {
                    url: "/client/game/start",
                    action: async (url, info, sessionId, output) => 
                    {
                        this.load();
                        return output;
                    }
                }
            ],
            "StatTrack"
        );

        staticRouterModService.registerStaticRouter(
            "StatTrack-StatTrackRoutes",
            [
                {
                    url: "/stattrack/save",
                    action: async (url, info: WeaponStats, sessionId, output) => this.saveWeaponStats(info)
                },
                {
                    url: "/stattrack/load",
                    action: async (url, info, sessionId, output) => JSON.stringify(this.weaponStats)
                }
            ],
            "StatTrack"
        );

        // listen to ItemHelper.replaceIDs to keep in sync
        container.afterResolution(
            "ItemHelper",
            (_, itemHelper: ItemHelper) => 
            {
                const originalReplaceIDs = itemHelper.replaceIDs;
                itemHelper.replaceIDs = (originalItems, pmcData, insuredItems, fastPanel) => 
                {
                    const results: IItem[] = originalReplaceIDs.call(
                        itemHelper,
                        originalItems,
                        pmcData,
                        insuredItems,
                        fastPanel
                    );

                    let dirty = false;
                    for (let i = 0; i < originalItems.length; i++) 
                    {
                        const oldId = originalItems[i]._id;
                        if (this.weaponStats == null) return results;
                        
                        for (const profileId of Object.keys(this.weaponStats))
                        {
                            if (oldId in this.weaponStats[profileId]) 
                            {
                                const newId = results[i]._id;
                                const weaponTpl = results[i]._tpl;
                                this.weaponStats[profileId][newId] = cloner.clone(this.weaponStats[profileId][oldId]);
                                this.weaponStats[profileId][newId].timesLost++;
                                this.weaponStats[profileId][weaponTpl].timesLost++;

                                dirty = true;
                                this.logger.debug(`[StatTrack] Weapon ${oldId} is now ${newId}, stats copied`);
                                delete this.weaponStats[profileId][oldId];
                            }
                        }
                    }

                    if (dirty) 
                    {
                        this.save();
                    }

                    return results;
                };
            },
            { frequency: "Always" }
        );
    }

    private async saveWeaponStats(payload: WeaponStats): Promise<string> 
    {
        if (!payload) 
        {
            this.logger.error("StatTrack: Bad save payload!");
            return;
        }
        else 
        {
            this.weaponStats = payload;
        }

        await this.save();
        return JSON.stringify({ success: true });
    }

    private async save() 
    {
        try 
        {
            await this.writeBackup();
            const filename = path.join(__dirname, "../weaponStats.json");
            await this.fileSystem.writeJson(filename, this.weaponStats, 2);
        }
        catch (error) 
        {
            this.logger.error("StatTrack: Failed to save weapon stats! " + error);
        }
    }

    private async writeBackup() 
    {
        try 
        {
            const filename = path.join(__dirname, "../weaponStats.json");
            const backupname = path.join(__dirname, "../weaponStats.json.bak");
            await this.fileSystem.copy(filename, backupname);
        }
        catch (error) 
        {
            this.logger.error("StatTrack: Failed to backup weapon stats! " + error);
        }
    }

    private load()
    {
        const filename = path.join(__dirname,  "../weaponStats.json");
        if (this.fileSystem.exists(filename)) 
        {
            const jsonData = this.fileSystem.readJson(filename);
            for (const profileId of Object.keys(jsonData))
            {
                for (const weaponID of Object.keys(jsonData[profileId])) 
                {
                    if (jsonData[profileId][weaponID].timesLost == undefined) jsonData[profileId][weaponID].timesLost = 0;
                }
            }
            this.weaponStats = jsonData;
        }
        else 
        {
            this.weaponStats = {};
            this.fileSystem.writeJson(filename, this.weaponStats);
        }
    }
}

type CustomizedObject = {
    kills: number,
    headshots: number,
    totalShots: number,
    timesLost: number,
};

type WeaponStats = Record<string, Record<string, CustomizedObject>>;

export const mod = new StatTrack();
