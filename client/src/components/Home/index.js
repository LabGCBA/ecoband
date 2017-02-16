import React, { PureComponent } from 'react';
import { Toolbar, ToolbarGroup, ToolbarSeparator, ToolbarTitle } from 'material-ui/Toolbar';

import DateRangeIcon from 'material-ui/svg-icons/action/date-range';
import Firebase from 'firebase';
import IconButton from 'material-ui/IconButton';
import MuiThemeProvider from 'material-ui/styles/MuiThemeProvider';
import ReactEcharts from 'echarts-for-react';
import Toggle from 'material-ui/Toggle';
import UpdateIcon from 'material-ui/svg-icons/action/update';
import differenceInMilliseconds from 'date-fns/difference_in_milliseconds';
import differenceInSeconds from 'date-fns/difference_in_seconds';
import getMuiTheme from 'material-ui/styles/getMuiTheme';
import injectTapEventPlugin from 'react-tap-event-plugin';
import lightBaseTheme from 'material-ui/styles/baseThemes/lightBaseTheme';
import styles from './styles.scss';
import substractSeconds from 'date-fns/sub_seconds';

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
        borderColor: '#FF5D9E'
    },
    emphasis: {
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
    yAxis: [
        {
            type: 'value',
            max: 200,
            axisLine: {
                lineStyle: textStyle
            }
        }
    ],
    textStyle
};

class Home extends PureComponent {
    constructor(props) {
        injectTapEventPlugin();
        super(props);

        Firebase.initializeApp({
            authDomain: 'ecoband-5e79f.firebaseapp.com',
            databaseURL: 'https://ecoband-5e79f.firebaseio.com'
        });

        this._device = 'C8:0F:10:80:DA:BE';
        this._database = Firebase.database();
        this._ref = this._database.ref(`${this._device}/activity`);
        this._heartBeatsToShow = 25;
        this._stepsToShow = 25;
        this.state = {
            beatsPerMinute: [],
            stepsPerMinute: [],
            lastBeat: new Date(),
            lastStep: new Date(),
            isBeatsChartLoading: true,
            isStepsChartLoafing: false
        };

        this._ref.limitToLast(this._heartBeatsToShow).on('child_added', this.onItemAdded.bind(this));
    }

    onItemAdded(record) {
        const data = record.val();
        const item = data.value;
        const timestamp = new Date(data.timestamp);
        const lastItem = this.state.beatsPerMinute[this.state.beatsPerMinute.length - 1];
        const now = Date.now();
        const newArray = [...this.state[data.type]];
        let newItem = [timestamp, item];

        // Is old?
        if (differenceInSeconds(now, newItem[0]) > 70) newItem = [null, null];
        // Is an outlier? (is the new item older that the last one?)
        else if (lastItem && differenceInMilliseconds(lastItem[0], newItem[0]) > 0) return;
        else if (data.type === 'beatsPerMinute') {
            if ((newArray.length >= this._heartBeatsToShow)) newArray.shift();

            this.setState({ lastBeat: newItem[0] });
        }
        else if (data.type === 'stepsPerMinute') {
            if (newArray.length >= this._stepsToShow) newArray.shift();

            this.setState({ lastStep: newItem[0] });
        }

        newArray.push(newItem);
        this.setState({ [data.type]: newArray });
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

        this.onItemAdded(item);
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
            xAxis: [
                {
                    type: 'time',
                    min: this.state.beatsPerMinute[0] ? this.state.beatsPerMinute[0][0] : new Date(),
                    max: this.state.lastBeat,
                    splitNumber: 5,
                    minInterval: 5,
                    axisLine: {
                        lineStyle: textStyle
                    }
                }
            ],
            series: [
                {
                    name: 'Pulsaciones',
                    type: 'line',
                    data: this.state.beatsPerMinute,
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
            xAxis: [
                {
                    type: 'time',
                    min: this.state.stepsPerMinute[0] ? this.state.stepsPerMinute[0][0] : new Date(),
                    max: this.state.lastStep,
                    splitNumber: 5,
                    minInterval: 5,
                    axisLine: {
                        lineStyle: textStyle
                    }
                }
            ],
            series: [
                {
                    name: 'Pulsaciones',
                    type: 'line',
                    data: this.state.stepsPerMinute,
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
            }
        };

        return (
          <MuiThemeProvider muiTheme={getMuiTheme(lightBaseTheme)}>
            <section id="main" style={style.main}>
              <Toolbar style={style.toolbar}>
                <ToolbarGroup style={style.toolbarGroup}>
                  <ToolbarTitle text="Ecoband" style={style.toolbarTitle} />
                  <IconButton style={style.iconButton}>
                    <DateRangeIcon color={'#F0EAFF'} hoverColor={'#FF5D9E'} />
                  </IconButton>
                  <IconButton style={style.iconButton}>
                    <UpdateIcon color={'#F0EAFF'} hoverColor={'#FF5D9E'} />
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
            </section>
          </MuiThemeProvider>
        );
    }
}

Home.childContextTypes = {
    muiTheme: React.PropTypes.object
};

export default Home;
