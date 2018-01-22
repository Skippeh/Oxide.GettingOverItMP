import * as React from 'react';
import ModVersionModel, { ModType } from '../models/mod-version';
import PropTypes from 'prop-types';
import Moment from 'moment';
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
	responseLoading: boolean;
	file: File;
	fileError: string;
	history: ModVersionModel[]
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
			responseLoading: false,
			file: null,
			fileError: null,
			history: []
		};

		this.onVersionChange = this.onVersionChange.bind(this);
		this.onSubmit = this.onSubmit.bind(this);
		this.onFileChange = this.onFileChange.bind(this);
	}

	componentDidMount(): void
	{
		this.loadVersionAsync();
	}

	async loadVersionAsync(): Promise<void>
	{
		this.setState({ version: null, history: null, loading: true });

		let version: ModVersionModel;
		let history: ModVersionModel[];

		try
		{
			version = await ClientApi.requestVersionAsync(this.props.type);
		}
		catch (error)
		{
			version = null;
		}

		try
		{
			history = await ClientApi.requestVersionHistory(this.props.type);
		}
		catch (error)
		{
			history = [];
		}
		this.setState({ loading: false, version, history });
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
				{this.renderHistory()}
			</div>
		);
	}

	private renderTitle(): React.ReactNode
	{
		const title: string = Utility.getFriendlyModType(this.props.type);

		return (
			<div>
				<h2>{title}</h2>
			</div>
		);
	}

	private renderInfo(): React.ReactNode
	{
		const releaseDate: Date = this.state.version && this.state.version.releaseDate || null;
		const moment = releaseDate && Moment(releaseDate);

		const versionNode = this.state.version == null
										 ? <span>Not found</span>
										 : <span>{this.state.version.version}</span>;

		const releaseDateNode = this.state.version == null
											 ? <span>-</span>
											 : <span title={moment.format('LLLL')}>{moment.calendar()}</span>;

		return (
			<div>
				<p>
					Current version: {versionNode}<br />
					Release date: {releaseDateNode}
				</p>
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
				<input type="text" placeholder="Version" onChange={this.onVersionChange}/>
				<span className='text-danger'>&nbsp;{this.state.inputVersionError}</span>
				<br />
				<input type="submit" value="Upload" onClick={(ev) => { ev.preventDefault(); this.onSubmit(); }} disabled={this.state.responseLoading} />
				<br />
				<span className="text-danger">{this.state.responseError}</span>
			</form>
		);
	}

	private renderHistory(): React.ReactNode
	{
		const renderModel = (model: ModVersionModel): React.ReactNode =>
		{
			const modTypeString = Utility.getFriendlyModType(model.type);

			const renderSuffix = (): React.ReactNode =>
			{
				if (this.state.version != null && this.state.version.version == model.version)
					return <span>(Current version)</span>;

				if (model.releaseDate > new Date())
					return <span>(Releases {Moment(model.releaseDate).fromNow()})</span>;

				return null;
			}

			const renderText = (): React.ReactNode =>
			{
				if (this.state.version.version == model.version)
					return <b>{model.version} {renderSuffix()}</b>;
				else
					return <span>{model.version} {renderSuffix()}</span>;
			};

			return (
				<a key={model.version} href={`${ClientApi.ApiUrl}/version/${modTypeString}/${model.version}/archive/all`}>{renderText()}</a>
			);
		}

		let map: React.ReactNode[] = this.state.history.map(model => renderModel(model));
		let history: React.ReactNode = null;

		if (map.length > 0)
			history = map.reduce((prev, curr) => [prev, <br />, curr]);

		return (
			<div>
				<h4>History</h4>
				{history}
			</div>
		)
	}

	private renderLoading(): React.ReactNode
	{
		return <h2>Loading {Utility.getFriendlyModType(this.props.type)}...</h2>;
	}

	private onVersionChange(ev): void
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
		else
		{
			await this.setState({ inputVersionError: null });
		}

		if (this.state.file == null)
		{
			await this.setState({ fileError: 'A file is required.' });
		}
		else
		{
			await this.setState({ fileError: null });
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

		await this.setState({ responseError: null, responseLoading: true });

		try
		{
			const response = await ClientApi.uploadVersionAsync(this.state.file, this.state.inputVersion, this.props.type, new Date());
			this.loadVersionAsync();
		}
		catch (error)
		{
			const response = error as Response;
			if (response.status != 400) // If not bad request
			{
				this.setState({ responseError: response.statusText });
			}
			else
			{
				const json = await response.json();
				this.setState({ responseError: json.error });
			}
		}
		finally
		{
			this.setState({ responseLoading: false });
		}
	}
}
