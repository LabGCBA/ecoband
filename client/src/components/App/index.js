import React, { PropTypes } from 'react';

import styles from './styles.scss';

function App({ children }) {
    return (
      <div>
        {children}
      </div>
    );
}

App.propTypes = {
    children: PropTypes.node
};

export default App;
