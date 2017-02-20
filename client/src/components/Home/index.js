import { Card, CardActions, CardHeader } from 'material-ui/Card';
import { CircularProgress, FlatButton } from 'material-ui';
import React, { Component } from 'react';

import Charts from '../Charts';
import DateRangePicker from 'react-daterange-picker';
import Firebase from 'firebase';
import Modal from 'simple-react-modal';
import Moment from 'moment';
import MuiThemeProvider from 'material-ui/styles/MuiThemeProvider';
import Toolbar from '../Toolbar';
import { extendMoment } from 'moment-range';
import getMuiTheme from 'material-ui/styles/getMuiTheme';
import injectTapEventPlugin from 'react-tap-event-plugin';
import lightBaseTheme from 'material-ui/styles/baseThemes/lightBaseTheme';
import styles from './styles.scss';

const moment = extendMoment(Moment);
const primaryColor = '#FF5D9E';
const secondaryColor = '#F0EAFF';

const textStyle = {
    color: secondaryColor,
    fontFamily: 'Roboto, Helvetica, Arial, sans-serif'
};

const lineStyle = {
    normal: {
        color: primaryColor
    },
    emphasis: {
        color: secondaryColor
    }
};

const itemStyle = {
    normal: {
        color: primaryColor,
        borderColor: primaryColor
    },
    emphasis: {
        color: secondaryColor,
        borderColor: secondaryColor
    }
};

const baseChartOptions = {
    animation: false,
    color: [
        primaryColor, primaryColor, primaryColor, secondaryColor, secondaryColor,
        '#749f83', '#ca8622', '#bda29a', '#6e7074', '#546570',
        '#c4ccd3'
    ],
    tooltip: {
        trigger: 'axis',
        axisPointer: {
            lineStyle: lineStyle.emphasis
        }
    },
    toolbox: {
        show: true,
        feature: {
            saveAsImage: {
                show: true,
                title: ' '
            }
        },
        iconStyle: {
            normal: {
                borderColor: secondaryColor
            },
            emphasis: {
                borderColor: primaryColor
            }
        }
    },
    grid: {
        show: false
    },
    xAxis: [
        {
            type: 'time',
            axisLine: {
                lineStyle: textStyle
            },
            axisLabel: {
                show: false
            },
            axisTick: {
                show: false
            },
            splitLine: {
                show: false
            }
        }
    ],
    yAxis: [
        {
            type: 'value',
            max: 200,
            axisLine: {
                lineStyle: textStyle
            },
            splitLine: {
                show: false
            }
        }
    ],
    textStyle
};

class Home extends Component {
    constructor(props) {
        injectTapEventPlugin();
        super(props);

        this._device = 'C8:0F:10:80:DA:BE';
        this._realTimeItems = 25;
        this._connections = 0;
        this.state = {
            beatsPerMinute: {
                list: [],
                last: moment(),
                limit: 25
            },
            stepsPerMinute: {
                list: [],
                last: moment(),
                limit: 25
            },
            realTime: true,
            loading: false,
            showDateRangeModal: false,
            dateRange: null,
            connected: true
        };
    }

    componentDidMount() {
        Firebase.initializeApp({
            authDomain: 'ecoband-5e79f.firebaseapp.com',
            databaseURL: 'https://ecoband-5e79f.firebaseio.com'
        });

        this._database = Firebase.database();

        this._database.ref(`${this._device}/activity`)
            .limitToLast(1)
            .on('child_added', this.onItemAddedRealTime.bind(this));
        this._database.ref('.info/connected')
            .on('value', this.onFirebaseConnectionStateChanged.bind(this));
    }

    onFirebaseConnectionStateChanged(connected) {
        if (connected.val()) {
            this._connections++;

            this.setState({ connected: true });
        }
        else if (this._connections > 0) this.setState({ connected: false });
    }

    onItems(records) {
        const results = {};
        let item;
        let newItem;
        let data;

        for (const key in records) {
            if (records.hasOwnProperty(key)) {
                data = records[key];
                item = data.value;
                newItem = [new Date(data.timestamp), item];
                data.type = data.type.trim();

                if (this.state.dateRange.contains(newItem[0])) {
                    if (!results[data.type]) results[data.type] = [];

                    results[data.type].push(newItem);
                }
            }
        }

        this.setChartsData(results);
    }

    onItemAddedRealTime(record) {
        const data = record.val();
        const item = data.value;
        const newArray = [...this.state[data.type].list];
        const currentItems = this.state[data.type].list.length;
        let newItem = [new Date(data.timestamp), item];
        let lastItem;

        if (!this.state.realTime) return;

        if (currentItems > 0) {
            for (let i = currentItems - 1; i >= 0; i--) {
                if (this.state[data.type].list[i][0]) {
                    lastItem = this.state[data.type].list[i];

                    break;
                }
            }
        }

        // Is old?
        if (moment.range(newItem[0], moment().toDate()).diff('seconds', false) > 70) newItem = [null, null];
        // Is an outlier? (is the new item older that the last one?)
        else if (lastItem && moment(lastItem[0]).isAfter(newItem[0])) return;

        if ((newArray.length >= this.state[data.type].limit)) newArray.shift();

        newArray.push(newItem);
        this.setChartData(data.type, newItem, newArray);
    }

    onChartReady(echartsInstance, type) {
        const item = {
            val: () => {
                return {
                    value: null,
                    timestamp: null,
                    type
                };
            }
        };

        this.onItemAddedRealTime(item);
    }

    onRealTimeButtonClick() {
        this.clearData();
        this.setState({ realTime: true });

        this._database.ref(`${this._device}/activity`)
            .limitToLast(this._realTimeItems)
            .on('child_added', this.onItemAddedRealTime.bind(this));
    }

    onDateRangeButtonClick() {
        if (this.state.realTime) this.clearData();
        this.setState({ realTime: false, showDateRangeModal: true });
    }

    onDateRangeModalClose() {
    }

    onDateRangeModalCancelButtonClick() {
        this.setState({ showDateRangeModal: false });
    }

    onDateRangeModalOkButtonClick() {
        const range = moment.range(this.state.dateRange.start, this.state.dateRange.end);

        if (!range.end || !range.start) return;

        this.setState({ showDateRangeModal: false, loading: true });

        this._database.ref(`${this._device}/activity`)
            .orderByChild('timestamp')
            .startAt(range.start.valueOf())
            .endAt(range.end.valueOf())
            .once('value')
            .then((records) => {
                const results = records.val();

                if (results) this.onItems(results);

                this.setState({ loading: false });
            });
    }

    onDateRangeSelected(range) {
        if (this.state.dateRange || !this.state.dateRange || !(this.state.dateRange.isEqual(range))) {
            range.end.add(23, 'hours');
            range.end.add(59, 'minutes');
            range.end.add(59, 'seconds');

            this.setState({ dateRange: range });
        }
    }

    singleCurry(func, curriedParam) {
        return (closureParam) => {
            func.bind(this)(closureParam, curriedParam);
        };
    }

    getBeatsChartOptions() {
        const customOptions = {
            title: {
                text: 'Pulsaciones por minuto',
                textStyle
            },
            dataZoom: {
                show: !this.state.realTime,
                showDetail: false,
                showDataShadow: false,
                handleStyle: {
                    color: secondaryColor,
                    borderColor: secondaryColor
                }
            },
            series: [
                {
                    name: 'Pulsaciones',
                    type: this.state.realTime ? 'line' : 'scatter',
                    data: this.state.beatsPerMinute.list,
                    connectNulls: false,
                    symbolSize: 5,
                    itemStyle,
                    lineStyle
                }
            ]
        };

        return Object.assign({}, baseChartOptions, customOptions);
    }

    getStepsChartOptions() {
        const customOptions = {
            title: {
                text: 'Pasos por minuto',
                textStyle
            },
            dataZoom: {
                show: !this.state.realTime,
                showDetail: false,
                showDataShadow: false,
                handleStyle: {
                    color: secondaryColor,
                    borderColor: secondaryColor
                }
            },
            series: [
                {
                    name: 'Pulsaciones',
                    type: this.state.realTime ? 'line' : 'scatter',
                    data: this.state.stepsPerMinute.list,
                    connectNulls: false,
                    symbolSize: 5,
                    itemStyle,
                    lineStyle
                }
            ]
        };

        return Object.assign({}, baseChartOptions, customOptions);
    }

    getChildContext() {
        return {
            muiTheme: getMuiTheme()
        };
    }

    setChartsData(data) {
        const keys = Object.getOwnPropertyNames(data);

        for (const type of keys) {
            const lastItem = data[type][data[type].length - 1];

            this.setChartData(type, lastItem, data[type]);
        }
    }

    setChartData(type, newItem, newArray) {
        const newState = Object.assign({}, this.state);

        newState[type].list = newArray;
        newState[type].last = newItem[0];

        this.setState({ [type]: newState[type] });
    }

    clearData() {
        const newState = Object.assign({}, this.state);

        newState.beatsPerMinute.list = [];
        newState.stepsPerMinute.list = [];

        this.setState({ beatsPerMinute: newState.beatsPerMinute, stepsPerMinute: newState.stepsPerMinute });
    }

    render() {
        const style = {
            main: {
                width: '75%',
                fontWeight: 'bold'
            },
            content: {
                marginTop: '3rem'
            },
            modal: {
                height: '100%',
                background: 'rgba(0, 0, 0, 0.5)',
                transition: 'opacity 0.3s ease-in',
                display: 'flex',
                justifyContent: 'center',
                alignItems: 'center'
            },
            dateRangeModalContainer: {
                padding: '0rem',
                background: 'none',
                width: '315px',
                display: 'flex',
                justifyContent: 'center',
                alignItems: 'center'
            },
            spinnerModalContainer: {
                padding: '0rem',
                background: 'none',
                width: '75px',
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
          <MuiThemeProvider muiTheme={getMuiTheme(lightBaseTheme)}>
            <section id="main" style={style.main}>
              <Toolbar
                onDateRangeButtonClick={this.onDateRangeButtonClick.bind(this)}
                onRealTimeButtonClick={this.onRealTimeButtonClick.bind(this)}
                realTime={this.state.realTime}
                primaryColor={primaryColor}
                secondaryColor={secondaryColor}
              />
              <Charts
                style={style.content}
                options={[
                    this.getBeatsChartOptions(),
                    this.getStepsChartOptions()
                ]}
                onChartReadyCallbacks={[
                    this.singleCurry.bind(this)(this.onChartReady, 'beatsPerMinute'),
                    this.singleCurry.bind(this)(this.onChartReady, 'stepsPerMinute')
                ]}
              />
              <Modal
                show={this.state.loading}
                transitionSpeed={100}
                closeOnOuterClick={false}
                containerStyle={style.dateRangeModalContainer}
                style={style.modal}
              >
                <CircularProgress
                  color={primaryColor}
                  size={75}
                />
              </Modal>
              <Modal
                show={this.state.showDateRangeModal}
                onClose={this.onDateRangeModalClose.bind(this)}
                transitionSpeed={100}
                closeOnOuterClick={false}
                containerStyle={style.spinnerModalContainer}
                style={style.modal}
              >
                <Card style={style.card} className="card">
                  <CardHeader title="Rango de fechas" />
                  <DateRangePicker
                    firstOfWeek={0}
                    numberOfCalendars={1}
                    selectionType="range"
                    showLegend={false}
                    onSelect={this.onDateRangeSelected.bind(this)}
                    value={this.state.dateRange}
                    maximumDate={moment().toDate()}
                    singleDateRange
                  />
                  <CardActions>
                    <FlatButton
                      label="Cancelar"
                      onClick={this.onDateRangeModalCancelButtonClick.bind(this)}
                      hoverColor={primaryColor}
                    />
                    <FlatButton
                      label="Aceptar"
                      onClick={this.onDateRangeModalOkButtonClick.bind(this)}
                      hoverColor={primaryColor}
                    />
                  </CardActions>
                </Card>
              </Modal>
              <Modal
                show={!this.state.connected}
                transitionSpeed={100}
                closeOnOuterClick={false}
                containerStyle={style.dateRangeModalContainer}
                style={style.modal}
              >
                <Card style={style.card} className="card">
                  <CardHeader title="Sin conexión a internet" />
                </Card>
              </Modal>
            </section>
          </MuiThemeProvider>
        );
    }
}

Home.childContextTypes = {
    muiTheme: React.PropTypes.object
};

export default Home;
