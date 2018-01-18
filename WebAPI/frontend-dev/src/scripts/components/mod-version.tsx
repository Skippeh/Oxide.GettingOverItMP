import * as React from 'react';
import ModVersionModel, { ModType } from '../models/mod-version';
import PropTypes from 'prop-types';
import ClientApi from '../client-api';
import Utility from '../utility';

interface Props extends React.Props<ModVersion>
{
	type: ModType
}

interface State
{
	loading: boolean;
	error: any;
	version: ModVersionModel;
	inputVersion: string;
	inputVersionError: string;
	inputState: InputState;
	responseError: string;
}

enum InputState
{
	Pristine,
	Valid,
	Invalid
}

export default class ModVersion extends React.Component<Props, State>
{
	constructor(props)
	{
		super(props);

		this.state = {
			loading: true,
			error: null,
			version: null,
			inputVersion: '',
			inputVersionError: null,
			inputState: InputState.Pristine,
			responseError: null
		};

		this.handleVersionChange = this.handleVersionChange.bind(this);
		this.onSubmit = this.onSubmit.bind(this);
	}

	componentDidMount(): void
	{
		this.loadVersion();
	}

	loadVersion(): void
	{
		ClientApi.requestVersionAsync(this.props.type).then((version) =>
		{
			this.setState({ loading: false, version });
		})
		.catch(err =>
		{
			this.setState({ loading: false, error: err});
			console.error('Failed to query version:', err);
		});
	}

	render(): React.ReactNode
	{
		return (
			<div className="modversion">
				{
					this.state.loading
						? this.renderLoading()
						: this.renderContent()
				}
			</div>
		);
	}

	private renderContent(): React.ReactNode
	{
		return (
			<div>
				{this.renderTitle()}
				{this.renderInfo()}
				{this.renderInput()}
			</div>
		);
	}

	private renderTitle(): React.ReactNode
	{
		let title: string = Utility.getFriendlyModType(this.props.type);
		return <h2>{title}</h2>;
	}

	private renderInfo(): React.ReactNode
	{
		return (
			<div>
				<p>Current version: {this.state.version.version}</p>
			</div>
		);
	}

	private renderInput(): React.ReactNode
	{
		return (
			<form className="input">
				<h4>Upload version</h4>
				<input type="file" />
				<br />
				<input type="text" placeholder="Version" onChange={this.handleVersionChange}/>
				<span className='text-danger'>&nbsp;{this.state.inputVersionError}</span>
				<br />
				<input type="submit" onClick={(ev) => { ev.preventDefault(); this.onSubmit(); }} />
				<br />
				<span className='text-danger'>{this.state.responseError}</span>
			</form>
		);
	}

	private renderLoading(): React.ReactNode
	{
		return <h2>Loading {Utility.getFriendlyModType(this.props.type)}...</h2>;
	}

	private handleVersionChange(ev): void
	{
		this.setState({
			inputVersion: ev.target.value
		}, () => this.validateInputAsync());
	}

	private async validateInputAsync(): Promise<void>
	{
		const version = this.state.inputVersion;

		if (version == '')
		{
			await this.setState({ inputVersionError: 'Version can\'t be empty.'});
		}
		else if (this.props.type == ModType.Client)
		{
			let versions: string[] = version.split(/_/gi);

			if (versions.length != 2)
			{
				await this.setState({ inputVersionError: 'Invalid version format, expected modversion_gameversion.'});
			}
			else
			{
				await this.setState({ inputVersionError: null });
			}
		}

		await this.setState({
			inputState: this.state.inputVersionError == null ? InputState.Valid : InputState.Invalid
		});
	}

	private onSubmit(): void
	{
		this.validateInputAsync().then(() =>
		{
			console.log(this.state);

			if (this.state.inputState != InputState.Valid)
				return;
		});
	}
}
