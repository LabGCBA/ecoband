import React, { PureComponent } from 'react';

import Firebase from 'firebase';
import ReactEcharts from 'echarts-for-react';
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
        const newItem = [timestamp, item];
        const lastItem = this.state.beatsPerMinute[this.state.beatsPerMinute.length - 1];
        let newArray;

        if (data.type === 'beatsPerMinute') {
            if (this.state.beatsPerMinute.length > 0) {
                if (!this.isValidData(newItem, lastItem)) return;

                newArray = [...this.state.beatsPerMinute];
                if (newArray.length >= this._heartBeatsToShow) newArray.shift();
            }
            else newArray = [];

            newArray.push(newItem);
            this.setState({ beatsPerMinute: newArray });
        }
        else if (data.type === 'stepsPerMinute') {
            if (this.state.stepsPerMinute.length > 0) {
                if (!this.isValidData(newItem, lastItem)) return;

                newArray = [...this.state.stepsPerMinute];
                if (newArray.length >= this._stepsToShow) newArray.shift();
            }
            else newArray = [];

            newArray.push(newItem);
            this.setState({ stepsPerMinute: newArray });
        }
    }

    onChartReadyCallback(chart) {
    }

    isValidData(newItem, lastItem) {
        const isOldData = differenceInSeconds(Date.now(), newItem[0]) > 30;
        const isOutlier = differenceInSeconds(Date.now(), lastItem[0]) < 0;

        return !(isOldData || isOutlier);
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
            xAxis: [
                {
                    type: 'time',
                    splitNumber: 15
                }
            ],
            yAxis: [
                {
                    type: 'value'
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
