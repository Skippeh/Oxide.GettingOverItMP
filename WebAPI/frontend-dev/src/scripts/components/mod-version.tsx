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
	file: File;
	fileError: string;
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
			responseError: null,
			file: null,
			fileError: null
		};

		this.handleVersionChange = this.handleVersionChange.bind(this);
		this.onSubmit = this.onSubmit.bind(this);
		this.onFileChange = this.onFileChange.bind(this);
	}

	componentDidMount(): void
	{
		this.loadVersionAsync();
	}

	async loadVersionAsync(): Promise<void>
	{
		this.setState({ version: null });

		var version = await ClientApi.requestVersionAsync(this.props.type);
		this.setState({ loading: false, version });
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
				<input type="file" onChange={this.onFileChange} />
				<span className='text-danger'>&nbsp;{this.state.fileError}</span>
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

	private onFileChange(ev): void
	{
		const file: File = ev.target.files[0];
		this.setState({ file });
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

		if (this.state.file == null)
		{
			await this.setState({ fileError: 'A file is required.' });
		}

		this.setState({
			inputState: this.state.inputVersionError == null && this.state.fileError == null ? InputState.Valid : InputState.Invalid
		});
	}

	private async onSubmit(): Promise<void>
	{
		await this.validateInputAsync();

		if (this.state.inputState != InputState.Valid)
			return;
	}
}
