import ModVersionModel, { ModType } from './models/mod-version';
import 'whatwg-fetch';
import Utility from './utility';

class ClientApi
{
	ApiUrl: string = 'https://api.gettingoverit.mp';

	async requestVersionAsync(modType: ModType, version: string = 'latest', includeUnreleased: boolean = false): Promise<ModVersionModel>
	{
		let modTypeString: string = Utility.getFriendlyModType(modType);
		let url = `${this.ApiUrl}/version/${modTypeString}`;

		if (includeUnreleased)
			url += '/all';

		const response = await fetch(url, { credentials: 'same-origin', cache: 'no-cache' });

		if (!response.ok)
			throw response;

		const json = await response.json();
		return new ModVersionModel(json);
	}

	async uploadVersionAsync(file: File, version: string, modType: ModType, releaseDate: Date): Promise<void>
	{
		const data = {
			releaseDate,
			version
		};

		const formData = new FormData();
		formData.set('data', JSON.stringify(data));
		formData.set('file', file);

		const modTypeString: string = Utility.getFriendlyModType(modType);

		const response = await fetch(`${this.ApiUrl}/version/${modTypeString}/upload`, {
			method: 'POST',
			cache: 'no-cache',
			credentials: 'same-origin',
			body: formData
		});

		if (!response.ok)
			throw response;
	}

	async requestVersionHistory(modType: ModType): Promise<ModVersionModel[]>
	{
		const modTypeString: string = Utility.getFriendlyModType(modType);
		const response = await fetch(`${this.ApiUrl}/version/${modTypeString}/history/all`, { credentials: 'same-origin', cache: 'no-cache' });

		if (!response.ok)
			throw response;

		const jsonVersions = await response.json();

		const result: ModVersionModel[] = [];
		for (let i = 0; i < jsonVersions.length; ++i)
		{
			result.push(new ModVersionModel(jsonVersions[i]));
		}

		return result;
	}
}

export default new ClientApi();
