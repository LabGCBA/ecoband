import { Card, CardActions, CardHeader } from 'material-ui/Card';
import { CircularProgress, FlatButton, IconButton, Toggle } from 'material-ui';
import React, { Component } from 'react';
import { Toolbar, ToolbarGroup, ToolbarSeparator, ToolbarTitle } from 'material-ui/Toolbar';

import DateRangeIcon from 'material-ui/svg-icons/action/date-range';
import DateRangePicker from 'react-daterange-picker';
import Firebase from 'firebase';
import Modal from 'simple-react-modal';
import Moment from 'moment';
import MuiThemeProvider from 'material-ui/styles/MuiThemeProvider';
import ReactEcharts from 'echarts-for-react';
import UpdateIcon from 'material-ui/svg-icons/action/update';
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

        Firebase.initializeApp({
            authDomain: 'ecoband-5e79f.firebaseapp.com',
            databaseURL: 'https://ecoband-5e79f.firebaseio.com'
        });

        this._device = 'C8:0F:10:80:DA:BE';
        this._database = Firebase.database();
        this._realTimeItems = 25;
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
            connected: false
        };

        this._database.ref(`${this._device}/activity`)
            .limitToLast(1)
            .on('child_added', this.onItemAddedRealTime.bind(this));
        this._database.ref.child('.info/connected')
            .on('value', this.onFirebaseConnectionStateChanged.bind(this));
    }

    onItems(records) {
        const newArray = [];
        let newItem;
        let data;

        for (const key in records) {
            if (records.hasOwnProperty(key)) {
                data = records[key];

                const item = data.value;

                newItem = [new Date(data.timestamp * 1000), item];

                if (this.state.dateRange.contains(data.timestamp)) newArray.push(newItem);
            }
        }

        this.setChartData(data, newItem, newArray);
    }

    onFirebaseConnectionStateChanged(connected) {
        if (connected.val()) this.setState({ connected: true });
        else this.setState({ connected: false });
    }

    onItemAddedRealTime(record) {
        const data = record.val();
        const item = data.value;
        const newArray = [...this.state[data.type].list];
        const currentItems = this.state[data.type].list.length;
        let newItem = [new Date(data.timestamp * 1000), item];
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
        if (moment.range(newItem[0], Date.now()).diff('seconds', false) > 70) newItem = [null, null];
        // Is an outlier? (is the new item older that the last one?)
        else if (lastItem && moment(lastItem[0]).isAfter(newItem[0])) return;

        if ((newArray.length >= this.state[data.type].limit)) newArray.shift();

        newArray.push(newItem);
        this.setChartData(data, newItem, newArray);
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
        this.setLimit(this._realTimeItems);
        this.setState({ realTime: true });

        this._database.ref(`${this._device}/activity`)
            .limitToLast(1)
            .on('child_added', this.onItemAddedRealTime.bind(this));
    }

    onDateRangeButtonClick() {
        this.setLimit(50);
        this.setState({ realTime: false, showDateRangeModal: true });
    }

    onDateRangeModalClose() {
    }

    onDateRangeModalCancelButtonClick() {
        this.setState({ showDateRangeModal: false });
    }

    onDateRangeModalOkButtonClick() {
        const range = Object.assign({}, this.state.dateRange);

        this.setState({ showDateRangeModal: false, loading: true });

        if (range.start.isSame(range.end)) range.end.add(1, 'days');

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

    setChartData(data, newItem, newArray) {
        const newState = Object.assign({}, this.state);

        newState[data.type].list = newArray;
        newState[data.type].last = newItem[0];

        this.setState({ [data.type]: newState[data.type] });
    }

    setLimit(number) {
        const newState = Object.assign({}, this.state);

        newState.beatsPerMinute.limit = number;
        newState.beatsPerMinute.list = [];
        newState.stepsPerMinute.limit = number;
        newState.stepsPerMinute.list = [];

        this.setState({ beatsPerMinute: newState.beatsPerMinute, stepsPerMinute: newState.stepsPerMinute });
    }

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
                color: secondaryColor
            },
            iconButton: {
                float: 'right',
                marginTop: '0.25rem'
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
              <Toolbar style={style.toolbar}>
                <ToolbarGroup style={style.toolbarGroup}>
                  <ToolbarTitle text="Ecoband" style={style.toolbarTitle} />
                  <IconButton
                    style={style.iconButton}
                    tooltip="Rango de fechas"
                    tooltipPosition="bottom-center"
                    onClick={this.onDateRangeButtonClick.bind(this)}
                  >
                    <DateRangeIcon
                      color={this.state.realTime ? secondaryColor : primaryColor}
                    />
                  </IconButton>
                  <IconButton
                    style={style.iconButton}
                    tooltip="Tiempo real"
                    tooltipPosition="bottom-center"
                    onClick={this.onRealTimeButtonClick.bind(this)}
                  >
                    <UpdateIcon
                      color={this.state.realTime ? primaryColor : secondaryColor}
                    />
                  </IconButton>
                </ToolbarGroup>
              </Toolbar>
              <div style={style.content}>
                <ReactEcharts
                  option={this.getBeatsChartOptions()}
                  onChartReady={this.singleCurry.bind(this)(this.onChartReady, 'beatsPerMinute')}
                />
                <ReactEcharts
                  option={this.getStepsChartOptions()}
                  onChartReady={this.singleCurry.bind(this)(this.onChartReady, 'stepsPerMinute')}
                />
              </div>
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
