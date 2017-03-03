import React, { PropTypes, PureComponent } from 'react';

import ReactEcharts from 'echarts-for-react';

class Charts extends PureComponent {
    render() {
        return (
          <div className="charts" style={this.props.containerStyle}>
            <ReactEcharts
              className="chart"
              style={this.props.style}
              option={this.props.options.beats}
              onChartReady={this.props.onChartReady.steps}
            />
            <ReactEcharts
              className="chart"
              style={this.props.style}
              option={this.props.options.steps}
              onChartReady={this.props.onChartReady.steps}
            />
          </div >
        );
    }
}

Charts.propTypes = {
    containerStyle: PropTypes.object,
    style: PropTypes.object,
    options: PropTypes.object,
    onChartReady: PropTypes.object
};

export default Charts;
