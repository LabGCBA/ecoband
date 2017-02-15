import React, { PureComponent } from 'react';
import { Toolbar, ToolbarGroup, ToolbarSeparator, ToolbarTitle } from 'material-ui/Toolbar';

import Firebase from 'firebase';
import FontIcon from 'material-ui/FontIcon';
import MuiThemeProvider from 'material-ui/styles/MuiThemeProvider';
import ReactEcharts from 'echarts-for-react';
import Toggle from 'material-ui/Toggle';
import differenceInMilliseconds from 'date-fns/difference_in_milliseconds';
import differenceInSeconds from 'date-fns/difference_in_seconds';
import getMuiTheme from 'material-ui/styles/getMuiTheme';
import lightBaseTheme from 'material-ui/styles/baseThemes/lightBaseTheme';
import styles from './styles.scss';
import substractSeconds from 'date-fns/sub_seconds';

const textStyle = {
    color: '#F0EAFF',
    fontFamily: 'Roboto, Helvetica, Arial, sans-serif'
};

const baseChartOptions = {
    animation: false,
    tooltip: {
        trigger: 'axis'
    },
    toolbox: {
        show: true,
        feature: {
            saveAsImage: { show: true }
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
    textStyle
};

class Home extends PureComponent {
    constructor(props) {
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
            yAxis: [
                {
                    type: 'value',
                    max: 200,
                    axisLine: {
                        lineStyle: textStyle
                    }
                }
            ],
            series: [
                {
                    name: 'Pulsaciones',
                    type: 'line',
                    data: this.state.beatsPerMinute
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
            yAxis: [
                {
                    type: 'value',
                    max: 200,
                    axisLine: {
                        lineStyle: textStyle
                    }
                }
            ],
            series: [
                {
                    name: 'Pasos',
                    type: 'line',
                    data: this.state.stepsPerMinute
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
                boxShadow: '0 14px 28px rgba(0,0,0,0.30), 0 10px 10px rgba(0,0,0,0.30);',
                backgroundColor: '#27232F'
            },
            toolbarGroup: {
                width: '100%',
                display: 'block'
            },
            toolbarTitle: {
                color: '#F0EAFF'
            },
            icons: {
                float: 'right',
                color: '#F0EAFF'
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
                  <FontIcon className="material-icons" style={style.icons}>date_range</FontIcon>
                  <FontIcon className="material-icons" style={style.icons}>update</FontIcon>
                </ToolbarGroup>
              </Toolbar>
              <div style={style.content}>
                <ReactEcharts option={this.getBeatsChartOptions()} />
                <ReactEcharts option={this.getStepsChartOptions()} />
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
