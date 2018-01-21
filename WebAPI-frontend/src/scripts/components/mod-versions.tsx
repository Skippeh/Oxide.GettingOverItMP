import * as React from 'react';
import ModVersionModel, { ModType } from '../models/mod-version';
import ModVersion from './mod-version';

export default class ModVersions extends React.Component
{
	render(): React.ReactNode
	{
		return (
			<div className="container versions">
				<div className="row">
					<div className="col-sm">
						<ModVersion type={ModType.Client} />
					</div>
					<div className="col-sm">
						<ModVersion type={ModType.Server} />
					</div>
				</div>
			</div>
		);
	}
}
