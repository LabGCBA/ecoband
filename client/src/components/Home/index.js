import React, { PureComponent } from 'react';

import Firebase from 'firebase';
import ReactEcharts from 'echarts-for-react';
import differenceInMilliseconds from 'date-fns/difference_in_milliseconds';
import differenceInSeconds from 'date-fns/difference_in_seconds';
import styles from './styles.scss';
import substractSeconds from 'date-fns/sub_seconds';

const baseChartOptions = {
    animation: false,
    tooltip: {
        trigger: 'axis'
    },
    toolbox: {
        show: true,
        feature: {
            saveAsImage: { show: true }
        }
    },
    grid: {
        show: false
    }
};

export default class Home extends PureComponent {
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

    onChartReadyCallback(chart) {
    }

    getBeatsChartOptions() {
        const customOptions = {
            title: {
                text: 'Pulsaciones por minuto'
            },
            xAxis: [
                {
                    type: 'time',
                    min: this.state.beatsPerMinute[0] ? this.state.beatsPerMinute[0][0] : new Date(),
                    max: this.state.lastBeat,
                    splitNumber: 5,
                    minInterval: 5
                }
            ],
            yAxis: [
                {
                    type: 'value',
                    max: 200
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
                text: 'Pasos por minuto'
            },
            xAxis: [
                {
                    type: 'time',
                    min: this.state.stepsPerMinute[0] ? this.state.stepsPerMinute[0][0] : new Date(),
                    max: this.state.lastStep,
                    splitNumber: 5,
                    minInterval: 10
                }
            ],
            yAxis: [
                {
                    type: 'value',
                    max: 200
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

    render() {
        return (
          <section>
            <ReactEcharts
              option={this.getBeatsChartOptions()}
              onChartReady={this.onChartReadyCallback.bind(this)}
            />
            <ReactEcharts
              option={this.getStepsChartOptions()}
              onChartReady={this.onChartReadyCallback.bind(this)}
            />
          </section>
        );
    }
}
