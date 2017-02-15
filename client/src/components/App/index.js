import React, { PropTypes } from 'react';

import styles from './styles.scss';

function App({ children }) {
    const style = {
        app: {
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center'
        }
    };

    return (
      <div style={style.app}>
        {children}
      </div>
    );
}

App.propTypes = {
    children: PropTypes.node
};

export default App;
