import ModVersionModel, { ModType } from './models/mod-version';
import 'whatwg-fetch';
import Utility from './utility';

class ClientApi
{
	static ApiUrl: string = 'http://skippy.pizza:8090';

	async requestVersionAsync(modType: ModType, version: string = 'latest'): Promise<ModVersionModel>
	{
		let modTypeString: string = Utility.getFriendlyModType(modType);

		const response = await fetch(`${ClientApi.ApiUrl}/version/${modTypeString}`);
		const json = await response.json();
		return new ModVersionModel(json);
	}
}

export default new ClientApi();
