import { Toolbar as MaterialToolbar, ToolbarGroup, ToolbarTitle } from 'material-ui/Toolbar';
import React, { PropTypes, PureComponent } from 'react';

import DateRangeIcon from 'material-ui/svg-icons/action/date-range';
import { IconButton } from 'material-ui';
import UpdateIcon from 'material-ui/svg-icons/action/update';

class Toolbar extends PureComponent {
    render() {
        const style = {
            main: {
                width: '75%',
                fontFamily: 'Roboto, \'Helvetica Neue\', Helvetica, Arial, sans-serif',
                fontWeight: 'bold'
            },
            toolbar: {
                boxShadow: '0 10px 20px rgba(0,0,0,0.19), 0 6px 6px rgba(0,0,0,0.22)',
                backgroundColor: '#27232F'
            },
            toolbarGroup: {
                width: '100%',
                display: 'block'
            },
            toolbarTitle: {
                color: this.props.secondaryColor
            },
            iconButton: {
                float: 'right',
                marginTop: '0.25rem'
            }
        };

        return (
          <MaterialToolbar style={style.toolbar}>
            <ToolbarGroup style={style.toolbarGroup}>
              <ToolbarTitle text="Ecoband" style={style.toolbarTitle} />
              <IconButton
                style={style.iconButton}
                tooltip="Rango de fechas"
                tooltipPosition="bottom-center"
                onClick={this.props.onDateRangeButtonClick}
              >
                <DateRangeIcon
                  color={this.props.realTime ? this.props.secondaryColor : this.props.primaryColor}
                />
              </IconButton>
              <IconButton
                style={style.iconButton}
                tooltip="Tiempo real"
                tooltipPosition="bottom-center"
                onClick={this.props.onRealTimeButtonClick}
              >
                <UpdateIcon
                  color={this.props.realTime ? this.props.primaryColor : this.props.secondaryColor}
                />
              </IconButton>
            </ToolbarGroup>
          </MaterialToolbar>
        );
    }
}

Toolbar.propTypes = {
    onDateRangeButtonClick: PropTypes.func.isRequired,
    onRealTimeButtonClick: PropTypes.func.isRequired,
    realTime: PropTypes.bool.isRequired,
    primaryColor: PropTypes.string.isRequired,
    secondaryColor: PropTypes.string.isRequired
};

export default Toolbar;