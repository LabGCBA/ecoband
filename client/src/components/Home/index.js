import React, { PureComponent } from 'react';

import Firebase from 'firebase';
import ReactEcharts from 'echarts-for-react';
import differenceInMilliseconds from 'date-fns/difference_in_milliseconds';
import differenceInSeconds from 'date-fns/difference_in_seconds';
import styles from './styles.scss';

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
        let newItem = [timestamp, item];
        let newArray;

        // Is old?
        if (differenceInSeconds(now, newItem[0]) > 30) newItem = [null, null];
        // Is an outlier? (is the new item older that the last one?)
        else if (lastItem && differenceInMilliseconds(lastItem[0], newItem[0]) > 0) return;

        if (this.state[data.type].length > 0) {
            newArray = [...this.state[data.type]];
            if (data.type === 'beatsPerMinute' && (newArray.length >= this._heartBeatsToShow)) newArray.shift();
            else if (data.type === 'stepsPerMinute' && newArray.length >= this._stepsToShow) newArray.shift();
        }
        else newArray = [];

        newArray.push(newItem);
        this.setState({ [data.type]: newArray });
    }

    onChartReadyCallback(chart) {
    }

    getBeatsChartOptions() {
        return {
            title: {
                text: 'Pulsaciones por minuto',
                subtext: 'Datos en tiempo real'
            },
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
            },
            xAxis: [
                {
                    type: 'time',
                    splitNumber: 10,
                    minInterval: 3
                }
            ],
            yAxis: [
                {
                    type: 'value',
                    max: 150
                }
            ],
            series: [
                {
                    name: 'Pulsaciones',
                    type: 'line',
                    data: this.state.beatsPerMinute
                }
            ],
            animation: false
        };
    }

    render() {
        return (
          <section>
            <ReactEcharts
              option={this.getBeatsChartOptions()}
              onChartReady={this.onChartReadyCallback.bind(this)}
            />
          </section>
        );
    }
}
