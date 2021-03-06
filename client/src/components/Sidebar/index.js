import React, { PropTypes, PureComponent } from 'react';

import HeartIcon from '../HeartIcon';
import styles from './styles.scss';

class Sidebar extends PureComponent {
    render() {
        const style = {
            sidebar: {
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                flexDirection: 'column',
                height: '100%',
                width: '7%',
                boxShadow: '8px 0px 16px rgba(0,0,0,0.18), 5px 0px 5px rgba(0,0,0,0.20)',
                backgroundColor: '#27232F'
            },
            iconButton: {
                width: 'auto',
                height: 'auto',
                marginLeft: '5px',
                paddingLeft: '3px'
            }
        };

        return (
          <div className="sidebar" style={style.sidebar}>
            <span className="counter counter-beats">
              {this.props.latestBeat}
            </span>
            <HeartIcon
              width="59.8"
              height="143.65"
              primaryColor={this.props.primaryColor}
              secondaryColor={this.props.secondaryColor}
            />
            <span className="counter counter-steps">
              {this.props.latestStep}
            </span>
          </div>
        );
    }
}

Sidebar.propTypes = {
    primaryColor: PropTypes.string.isRequired,
    secondaryColor: PropTypes.string.isRequired,
    latestBeat: PropTypes.number,
    latestStep: PropTypes.number
};

export default Sidebar;
