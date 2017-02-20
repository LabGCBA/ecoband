import React, { PropTypes, PureComponent } from 'react';

import ReactEcharts from 'echarts-for-react';

class Charts extends PureComponent {
    render() {
        return (
          <div style={this.props.style}>
            <ReactEcharts
              option={this.props.options[0]}
              onChartReady={this.props.onChartReadyCallbacks[0]}
            />
            <ReactEcharts
              option={this.props.options[1]}
              onChartReady={this.props.onChartReadyCallbacks[1]}
            />
          </div >
        );
    }
}

Charts.propTypes = {
    style: PropTypes.Object,
    options: PropTypes.Array,
    onChartReadyCallbacks: PropTypes.Array
};

export default Charts;
