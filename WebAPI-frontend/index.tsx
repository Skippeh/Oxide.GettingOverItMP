import * as React from 'react';
import * as ReactDOM from 'react-dom';
import App from './src/scripts/app';

global.$ = global.jQuery = require('jquery');
import 'bootstrap/dist/js/bootstrap.bundle.js';
import 'bootstrap/dist/css/bootstrap.css';


ReactDOM.render(<App />, document.getElementById('app') as HTMLElement);
