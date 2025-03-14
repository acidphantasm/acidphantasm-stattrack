import { DependencyContainer } from "tsyringe";

import { IPostDBLoadMod } from "@spt/models/external/IPostDBLoadMod";

class StatTrack implements IPostDBLoadMod
{
    public postDBLoad(container: DependencyContainer): void
    {
        
    }
}

export const mod = new StatTrack();
