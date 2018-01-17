import * as React from 'react';
import * as ReactDOM from 'react-dom';
import App from './src/scripts/app';

import $ from 'jquery';
import 'bootstrap';
import 'bootstrap/dist/css/bootstrap.css';

ReactDOM.render(<App />, document.getElementById('app') as HTMLElement);