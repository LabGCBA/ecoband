import React, { PropTypes, PureComponent } from 'react';

class HeartIcon extends PureComponent {
    render() {
        return (
          <svg
            width={this.props.width}
            height={this.props.height}
            viewBox="163 34 184 442"
            xmlns="http://www.w3.org/2000/svg"
          >
            <defs>
              <path d="M163.49416 124c1.08106-49.8867 41.6953-90 91.64203-90 49.9467 0 90.56092 40.1133 91.62075 90l.02123 262c-1.08106 49.8867-41.6953 90-91.642 90-49.94676 0-90.561-40.1133-91.6208-90l-.02125-262z" id="a" />
              <mask id="b" x="0" y="0" width="183.28405" height="442" fill="#fff">
                <use xlinkHref="#a" />
              </mask>
            </defs>
            <use stroke="#FF5D9E" mask="url(#b)" strokeWidth="24" fill="none" xlinkHref="#a" />
            <g fill="#FF5D9E" fillRule="evenodd" transform="translate(197.23 212)">
              <ellipse cx="20.59283" cy="5.73333" rx="5.72023" ry="5.73333" />
              <ellipse cx="35.46542" cy="5.73333" rx="5.72023" ry="5.73333" />
              <ellipse cx="20.59283" cy="20.64" rx="5.72023" ry="5.73333" />
              <ellipse cx="5.72023" cy="20.64" rx="5.72023" ry="5.73333" />
              <ellipse cx="65.21062" cy="20.64" rx="5.72023" ry="5.73333" />
              <ellipse cx="35.46542" cy="20.64" rx="5.72023" ry="5.73333" />
              <ellipse cx="50.33802" cy="20.64" rx="5.72023" ry="5.73333" />
              <ellipse cx="20.59283" cy="35.54667" rx="5.72023" ry="5.73333" />
              <ellipse cx="5.72023" cy="35.54667" rx="5.72023" ry="5.73333" />
              <ellipse cx="65.21062" cy="35.54667" rx="5.72023" ry="5.73333" />
              <ellipse cx="35.46542" cy="35.54667" rx="5.72023" ry="5.73333" />
              <ellipse cx="50.33802" cy="35.54667" rx="5.72023" ry="5.73333" />
              <ellipse cx="20.59283" cy="50.45333" rx="5.72023" ry="5.73333" />
              <ellipse cx="65.21062" cy="50.45333" rx="5.72023" ry="5.73333" />
              <ellipse cx="35.46542" cy="50.45333" rx="5.72023" ry="5.73333" />
              <ellipse cx="50.33802" cy="50.45333" rx="5.72023" ry="5.73333" />
              <ellipse cx="65.21062" cy="65.36" rx="5.72023" ry="5.73333" />
              <ellipse cx="35.46542" cy="65.36" rx="5.72023" ry="5.73333" />
              <ellipse cx="50.33802" cy="65.36" rx="5.72023" ry="5.73333" />
              <ellipse cx="65.21062" cy="80.26667" rx="5.72023" ry="5.73333" />
              <ellipse cx="50.33802" cy="80.26667" rx="5.72023" ry="5.73333" />
              <ellipse cx="94.95581" cy="5.73333" rx="5.72023" ry="5.73333" />
              <ellipse cx="80.08321" cy="5.73333" rx="5.72023" ry="5.73333" />
              <ellipse cx="94.95581" cy="20.64" rx="5.72023" ry="5.73333" />
              <ellipse cx="80.08321" cy="20.64" rx="5.72023" ry="5.73333" />
              <ellipse cx="109.82841" cy="20.64" rx="5.72023" ry="5.73333" />
              <ellipse cx="94.95581" cy="35.54667" rx="5.72023" ry="5.73333" />
              <ellipse cx="80.08321" cy="35.54667" rx="5.72023" ry="5.73333" />
              <ellipse cx="109.82841" cy="35.54667" rx="5.72023" ry="5.73333" />
              <ellipse cx="94.95581" cy="50.45333" rx="5.72023" ry="5.73333" />
              <ellipse cx="80.08321" cy="50.45333" rx="5.72023" ry="5.73333" />
              <ellipse cx="80.08321" cy="65.36" rx="5.72023" ry="5.73333" />
            </g>
          </svg>
        );
    }
}

HeartIcon.propTypes = {
    width: PropTypes.number.isRequired,
    height: PropTypes.number.isRequired,
    primaryColor: PropTypes.string.isRequired,
    secondaryColor: PropTypes.string.isRequired
};

export default HeartIcon;
