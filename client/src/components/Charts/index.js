import React, { PropTypes, PureComponent } from 'react';

import ReactEcharts from 'echarts-for-react';

class Charts extends PureComponent {
    render() {
        return (
          <div style={this.props.style}>
            <ReactEcharts
              option={this.props.options.beats}
              onChartReady={this.props.onChartReady.steps}
            />
            <ReactEcharts
              option={this.props.options.steps}
              onChartReady={this.props.onChartReady.steps}
            />
          </div >
        );
    }
}

Charts.propTypes = {
    style: PropTypes.object,
    options: PropTypes.object,
    onChartReady: PropTypes.object
};

export default Charts;
