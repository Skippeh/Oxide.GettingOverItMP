import ModVersionModel, { ModType } from './models/mod-version';
import 'whatwg-fetch';
import Utility from './utility';

class ClientApi
{
	ApiUrl: string = 'http://localhost:8090';

	async requestVersionAsync(modType: ModType, version: string = 'latest'): Promise<ModVersionModel>
	{
		let modTypeString: string = Utility.getFriendlyModType(modType);

		const response = await fetch(`${this.ApiUrl}/version/${modTypeString}`);
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
}

export default new ClientApi();
