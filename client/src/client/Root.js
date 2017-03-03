import { Router, hashHistory } from 'react-router/lib';

import React from 'react';
import routes from '../routes';

// We need a Root component for React Hot Loading.
function Root() {
    return (
      <Router history={hashHistory} routes={routes} />
    );
}

export default Root;
