import { Card, CardActions, CardHeader } from 'material-ui/Card';
import React, { Component } from 'react';
import { Toolbar, ToolbarGroup, ToolbarSeparator, ToolbarTitle } from 'material-ui/Toolbar';

import DateRangeIcon from 'material-ui/svg-icons/action/date-range';
import DateRangePicker from 'react-daterange-picker';
import Firebase from 'firebase';
import FlatButton from 'material-ui/FlatButton';
import IconButton from 'material-ui/IconButton';
import Modal from 'simple-react-modal';
import Moment from 'moment';
import MuiThemeProvider from 'material-ui/styles/MuiThemeProvider';
import ReactEcharts from 'echarts-for-react';
import Toggle from 'material-ui/Toggle';
import UpdateIcon from 'material-ui/svg-icons/action/update';
import { extendMoment } from 'moment-range';
import getMuiTheme from 'material-ui/styles/getMuiTheme';
import injectTapEventPlugin from 'react-tap-event-plugin';
import lightBaseTheme from 'material-ui/styles/baseThemes/lightBaseTheme';
import styles from './styles.scss';

const moment = extendMoment(Moment);

const textStyle = {
    color: '#F0EAFF',
    fontFamily: 'Roboto, Helvetica, Arial, sans-serif'
};

const lineStyle = {
    normal: {
        color: '#FF5D9E'
    },
    emphasis: {
        color: '#F0EAFF'
    }
};

const itemStyle = {
    normal: {
        color: '#FF5D9E',
        borderColor: '#FF5D9E'
    },
    emphasis: {
        color: '#F0EAFF',
        borderColor: '#F0EAFF'
    }
};

const baseChartOptions = {
    animation: false,
    color: [
        '#FF5D9E', '#FF5D9E', '#FF5D9E', '#d48265', '#91c7ae',
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
                borderColor: '#F0EAFF'
            },
            emphasis: {
                borderColor: '#FF5D9E'
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
        this._heartBeatsToShow = 25;
        this._stepsToShow = 25;
        this._primaryColor = '#FF5D9E';
        this._secondaryColor = '#F0EAFF';
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
            showDateRangeModal: false,
            dateRange: null
        };

        this._database.ref(`${this._device}/activity`)
            .limitToLast(1)
            .on('child_added', this.onItemAddedRealTime.bind(this));
    }

    onItems(records) {
        const newArray = [];
        let newItem;
        let data;

        for (const key in records) {
            if (records.hasOwnProperty(key)) {
                data = records[key];

                const item = data.value;
                const timestamp = moment(data.timestamp);

                newItem = [timestamp.toDate(), item];

                if (this.state.dateRange.contains(timestamp)) newArray.push(newItem);
            }
        }

        this.setChartData(data, newItem, newArray);
    }

    onItemAddedRealTime(record) {
        const data = record.val();
        const item = data.value;
        const timestamp = moment(data.timestamp);
        const now = Date.now();
        const newArray = [...this.state[data.type].list];
        const currentItems = this.state[data.type].list.length;
        let newItem = [timestamp.toDate(), item];
        let lastItem;

        if (currentItems > 0) {
            for (let i = currentItems - 1; i >= 0; i--) {
                if (this.state[data.type].list[i][0]) {
                    lastItem = this.state[data.type].list[i];

                    break;
                }
            }
        }

        // Is old?
        if (moment.range(newItem[0], now).diff('seconds', false) > 70) newItem = [null, null];
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
        this.setLimit(this._heartBeatsToShow);
        this.setState({ realTime: true });

        this._database.ref(`${this._device}/activity`)
            .limitToLast(this._heartBeatsToShow)
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
        this.setState({ showDateRangeModal: false });

        this._database.ref(`${this._device}/activity`)
            .orderByChild('timestamp')
            .startAt(this.state.dateRange.start.valueOf())
            .endAt(this.state.dateRange.end.valueOf())
            .once('value')
            .then((records) => {
                const results = records.val();

                console.log(results);

                if (results) this.onItems(results);
            });

            /*
        this._database.ref(`${this._device}/activity`)
            .orderByChild('timestamp')
            .startAt(this.state.dateRange.start.valueOf())
            .endAt(this.state.dateRange.end.valueOf())
            .on('child_added', this.onItems.bind(this)); */
    }

    onDateRangeSelected(range) {
        if (range.start.isSame(range.end)) range.end.add(1, 'seconds');
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
                start: 75,
                end: 100
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
                start: 75,
                end: 100
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
                color: '#F0EAFF'
            },
            iconButton: {
                float: 'right',
                marginTop: '0.25rem'
            },
            content: {
                marginTop: '3rem'
            },
            modal: {
                background: 'rgba(0, 0, 0, 0.5)',
                transition: 'opacity 0.3s ease-in'
            },
            modalContainer: {
                padding: '0rem',
                background: 'none',
                width: '315px'
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
                      color={this.state.realTime ? this._secondaryColor : this._primaryColor}
                    />
                  </IconButton>
                  <IconButton
                    style={style.iconButton}
                    tooltip="Tiempo real"
                    tooltipPosition="bottom-center"
                    onClick={this.onRealTimeButtonClick.bind(this)}
                  >
                    <UpdateIcon
                      color={this.state.realTime ? this._primaryColor : this._secondaryColor}
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
                show={this.state.showDateRangeModal}
                onClose={this.onDateRangeModalClose.bind(this)}
                transitionSpeed={300}
                closeOnOuterClick={false}
                containerStyle={style.modalContainer}
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
                      hoverColor="#FF5D9E"
                    />
                    <FlatButton
                      label="Aceptar"
                      onClick={this.onDateRangeModalOkButtonClick.bind(this)}
                      hoverColor="#FF5D9E"
                    />
                  </CardActions>
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
