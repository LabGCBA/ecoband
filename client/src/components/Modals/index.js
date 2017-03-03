import { Card, CardActions, CardHeader } from 'material-ui/Card';
import { CircularProgress, FlatButton } from 'material-ui';
import React, { PropTypes, PureComponent } from 'react';

import DateRangePicker from 'react-daterange-picker';
import Modal from 'simple-react-modal';
import Moment from 'moment';
import { extendMoment } from 'moment-range';

const moment = extendMoment(Moment);

class Modals extends PureComponent {
    getConnectingModal(style) {
        return (
          <Modal
            show={this.props.values.spinner.show}
            transitionSpeed={100}
            closeOnOuterClick={false}
            containerStyle={style.container}
            style={style.modal}
          >
            <CircularProgress
              color={this.props.primaryColor}
              size={75}
            />
          </Modal>
        );
    }

    getDisconnectedModal(style) {
        return (
          <Modal
            show={this.props.values.connection.show}
            transitionSpeed={100}
            closeOnOuterClick={false}
            containerStyle={style.container}
            style={style.modal}
          >
            <Card style={style.card} className="card">
              <CardHeader title="Sin conexión a internet" />
            </Card>
          </Modal>
        );
    }

    getDateRangeModal(style) {
        return (
          <Modal
            show={this.props.values.dateRange.show}
            onClose={this.props.events.dateRange.onClose}
            transitionSpeed={100}
            closeOnOuterClick={false}
            containerStyle={style.container}
            style={style.modal}
          >
            <Card style={style.card} className="card">
              <CardHeader title="Rango de fechas" />
              <DateRangePicker
                firstOfWeek={0}
                numberOfCalendars={1}
                selectionType="range"
                showLegend={false}
                onSelect={this.props.events.dateRange.onSelect}
                value={this.props.values.dateRange.range}
                maximumDate={moment().toDate()}
                singleDateRange
              />
              <CardActions>
                <FlatButton
                  label="Cancelar"
                  onClick={this.props.events.dateRange.onCancelButtonClick}
                  hoverColor={this.props.primaryColor}
                />
                <FlatButton
                  label="Aceptar"
                  onClick={this.props.events.dateRange.onOkButtonClick}
                  hoverColor={this.props.primaryColor}
                />
              </CardActions>
            </Card>
          </Modal>
        );
    }

    render() {
        const style = {
            modal: {
                height: '100%',
                background: 'rgba(0, 0, 0, 0.5)',
                transition: 'opacity 0.3s ease-in',
                display: 'flex',
                justifyContent: 'center',
                alignItems: 'center'
            },
            container: {
                padding: '0rem',
                background: 'none',
                display: 'flex',
                justifyContent: 'center',
                alignItems: 'center'
            },
            card: {
                padding: '0rem',
                borderRadius: '0.35rem',
                background: '#2C2734'
            }
        };

        return (
          <div>
            {this.props.values.connection ? this.getConnectingModal(style) : '' }
            {this.props.values.connection ? this.getDisconnectedModal(style) : ''}
            {this.props.values.dateRange ? this.getDateRangeModal(style) : ''}
          </div>
        );
    }
}

Modals.propTypes = {
    primaryColor: PropTypes.string.isRequired,
    events: PropTypes.object,
    values: PropTypes.object
};

export default Modals;
