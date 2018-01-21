export enum ModType
{
	Invalid,
	Client,
	Server
}

export class FileChecksum
{
	filePath: string;
	md5: string;
	releaseDate: Date;

	constructor(filePath: string, md5: string)
	{
		this.filePath = filePath;
		this.md5 = md5;
	}
}

export default class ModVersionModel
{
	version: string;
	type: ModType;
	checksums: FileChecksum[];
	releaseDate: Date;

	constructor(json?: any)
	{
		if (json != null)
		{
			this.version = json.version;
			this.type = json.type;
			this.releaseDate = new Date(json.releaseDate);

			if (json.checksums != null)
			{
				this.checksums = [];
				for (let i = 0; i < json.checksums.length; ++i)
				{
					const jsonChecksum = json.checksums[i];
					this.checksums.push(new FileChecksum(jsonChecksum.filePath, jsonChecksum.md5));
				}
			}
		}
	}
}
