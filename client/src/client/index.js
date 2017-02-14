import { AppContainer } from 'react-hot-loader';
import React from 'react';
import Root from './Root';
import { render } from 'react-dom';

const root = document.querySelector('#root');

const mount = (RootComponent) => {
    render(
      <AppContainer>
        <RootComponent />
      </AppContainer>,
      root
    );
};

if (module.hot) {
    module.hot.accept('./Root', () => {
        // eslint-disable-next-line global-require,import/newline-after-import
        const RootComponent = require('./Root').default;
        mount(RootComponent);
    });
}

mount(Root);
