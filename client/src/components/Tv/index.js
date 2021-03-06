import React, { Component } from 'react';

import Charts from '../Charts';
import Firebase from 'firebase';
import Modals from '../Modals';
import Moment from 'moment';
import MuiThemeProvider from 'material-ui/styles/MuiThemeProvider';
import Sidebar from '../Sidebar';
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
    fontFamily: '\'Roboto\', \'Helvetica Neue\', Helvetica, Arial, sans-serif'
};

const titleStyle = {
    color: secondaryColor,
    fontFamily: '\'Rubik\', \'Helvetica Neue\', Helvetica, Arial, sans-serif',
    fontWeight: 400,
    fontSize: 16
};

const lineStyle = {
    normal: {
        color: primaryColor,
        width: 3
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
        show: false,
        left: '4%',
        right: '4%'
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
            offset: 1,
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
        this._items = 25;
        this._connections = 0;
        this.state = {
            beatsPerMinute: {
                list: [],
                latest: 0,
                last: moment(),
                limit: 25
            },
            stepsPerMinute: {
                list: [],
                latest: 0,
                last: moment(),
                limit: 25
            },
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

        if (newArray.length >= this._items) newArray.shift();

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
                text: 'PULSACIONES POR MINUTO',
                x: 'center',
                textStyle: titleStyle
            },
            series: [
                {
                    name: 'Pulsaciones',
                    type: 'line',
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
                text: 'PASOS POR MINUTO',
                x: 'center',
                textStyle: titleStyle
            },
            series: [
                {
                    name: 'Pulsaciones',
                    type: 'line',
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

        if (newItem[1] > 0) newState[type].latest = newItem[1];

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
                fontWeight: 'bold',
                width: '100%',
                height: '100vh',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'flex-start',
                overflow: 'hidden'
            },
            content: {
                width: '100%',
                height: '100vh',
                marginTop: '5px'
            },
            chart: {
                height: '50vh'
            }
        };

        return (
          <MuiThemeProvider muiTheme={getMuiTheme(lightBaseTheme)}>
            <section id="main" style={style.main}>
              <Sidebar
                primaryColor={primaryColor}
                secondaryColor={secondaryColor}
                latestBeat={this.state.beatsPerMinute.latest}
                latestStep={this.state.stepsPerMinute.latest}
              />
              <Charts
                containerStyle={style.content}
                style={style.chart}
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
