import React, { Component } from 'react';

import Charts from '../Charts';
import Firebase from 'firebase';
import Modals from '../Modals';
import Moment from 'moment';
import MuiThemeProvider from 'material-ui/styles/MuiThemeProvider';
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
    fontFamily: 'Roboto, \'Helvetica Neue\', Helvetica, Arial, sans-serif'
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
        show: false
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

class Tv extends Component {
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

    singleCurry(func, curriedParam) {
        return (closureParam) => {
            func.bind(this)(closureParam, curriedParam);
        };
    }

    getBeatsChartOptions() {
        const customOptions = {
            title: {
                text: 'Pulsaciones por minuto',
                x: 'center',
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
                x: 'center',
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
                width: '100%',
                fontWeight: 'bold'
            },
            content: {
                marginTop: '3rem'
            }
        };

        return (
          <MuiThemeProvider muiTheme={getMuiTheme(lightBaseTheme)}>
            <section id="main" style={style.main}>
              <Charts
                style={style.content}
                options={{
                    beats: this.getBeatsChartOptions(),
                    steps: this.getStepsChartOptions()
                }}
                onChartReady={{
                    beats: this.singleCurry.bind(this)(this.onChartReady, 'beatsPerMinute'),
                    steps: this.singleCurry.bind(this)(this.onChartReady, 'stepsPerMinute')
                }}
              />
              <Modals
                values={{
                    spinner: {
                        show: this.state.loading
                    },
                    connection: {
                        show: !this.state.connected
                    }
                }}
                primaryColor={primaryColor}
              />
            </section>
          </MuiThemeProvider>
        );
    }
}

Tv.childContextTypes = {
    muiTheme: React.PropTypes.object
};

export default Tv;
