import { Card, CardActions, CardHeader } from 'material-ui/Card';
import { CircularProgress, FlatButton } from 'material-ui';
import React, { PropTypes, PureComponent } from 'react';

import DateRangePicker from 'react-daterange-picker';
import Modal from 'simple-react-modal';
import Moment from 'moment';
import { extendMoment } from 'moment-range';

const moment = extendMoment(Moment);

class Modals extends PureComponent {
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
            <Modal
              show={this.props.show.spinner}
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
            <Modal
              show={this.props.show.dateRange}
              onClose={this.props.onClose.dateRange}
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
                  onSelect={this.props.onSelect.dateRange}
                  value={this.props.values.dateRange}
                  maximumDate={moment().toDate()}
                  singleDateRange
                />
                <CardActions>
                  <FlatButton
                    label="Cancelar"
                    onClick={this.props.onCancelButtonClick.dateRange}
                    hoverColor={this.props.primaryColor}
                  />
                  <FlatButton
                    label="Aceptar"
                    onClick={this.props.onOkButtonClick.dateRange}
                    hoverColor={this.props.primaryColor}
                  />
                </CardActions>
              </Card>
            </Modal>
            <Modal
              show={this.props.show.connection}
              transitionSpeed={100}
              closeOnOuterClick={false}
              containerStyle={style.container}
              style={style.modal}
            >
              <Card style={style.card} className="card">
                <CardHeader title="Sin conexión a internet" />
              </Card>
            </Modal>
          </div>
        );
    }
}

Modals.propTypes = {
    show: PropTypes.object.isRequired,
    primaryColor: PropTypes.string.isRequired,
    values: PropTypes.object,
    onSelect: PropTypes.object,
    onClose: PropTypes.object,
    onCancelButtonClick: PropTypes.object,
    onOkButtonClick: PropTypes.object
};

export default Modals;
