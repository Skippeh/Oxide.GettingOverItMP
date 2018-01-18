import * as React from 'react';
import '../scss/style.scss';
import { ModVersions } from './components';

export default class App extends React.Component
{
	render(): React.ReactNode
	{
		return (
			<div className="container">
				<ModVersions />
			</div>
		);
	}
}
