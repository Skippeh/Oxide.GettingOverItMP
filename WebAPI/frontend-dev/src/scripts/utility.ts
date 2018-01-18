import { ModType } from "./models/mod-version";

class Utility
{
	getFriendlyModType(modType: ModType): string
	{
		switch (modType)
		{
			default: throw new Error(`Not implemented: ${modType}`)
			case ModType.Invalid: return "Invalid";
			case ModType.Client: return "Client";
			case ModType.Server: return "Server";
		}
	}
}

export default new Utility();
