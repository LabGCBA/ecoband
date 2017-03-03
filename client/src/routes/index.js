import { IndexRoute, Route } from 'react-router/lib';

import App from '../components/App';
import React from 'react';

// Webpack 2 supports ES2015 `System.import` by auto-
// chunking assets. Check out the following for more:
// https://gist.github.com/sokra/27b24881210b56bbaff7#code-splitting-with-es6

const importHome = (nextState, cb) => {
    System.import('../components/Home')
      .then(module => cb(null, module.default))
      .catch((e) => {
          throw e;
      });
};

const importTv = (nextState, cb) => {
    System.import('../components/Tv')
      .then(module => cb(null, module.default))
      .catch((e) => {
          throw e;
      });
};


// We use `getComponent` to dynamically load routes.
// https://github.com/reactjs/react-router/blob/master/docs/guides/DynamicRouting.md
const routes = (
  <Route path="/" component={App}>
    <IndexRoute getComponent={importHome} />
    <Route path="tv" getComponent={importTv} />
  </Route>
);

// Unfortunately, HMR breaks when we dynamically resolve
// routes so we need to require them here as a workaround.
// https://github.com/gaearon/react-hot-loader/issues/288
if (module.hot) {
    require('../components/Home'); // eslint-disable-line global-require
    require('../components/Tv'); // eslint-disable-line global-require
}

export default routes;
