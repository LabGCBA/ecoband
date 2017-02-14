import React, { Component } from 'react';

import Datasource from './Datasource';
import styles from './styles.scss';

const datasource = new Datasource();

export default class Home extends Component {
    constructor(props) {
        super(props);

        this.state = {
            beatsPerMinute: datasource.beatsPerMinute
        };
    }

    render() {
        return (
          <section>
            <p className={styles.paragraph}>
              Welcome to the <strong>Static React Starter-kyt</strong>.
              This starter kyt should serve as the base for a client rendered React app.
            </p>
            <p className={styles.paragraph}>
              Check out the Tools section for an outline of the libraries that
              are used in this Starter-kyt.
            </p>
          </section>
        );
    }
}
