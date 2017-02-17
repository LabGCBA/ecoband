const HtmlWebpackPlugin = require('html-webpack-plugin');

module.exports = {
    reactHotLoader: true,
    debug: false,
    hasServer: false,
    modifyWebpackConfig: (config, options) => {
        if (options.type === 'client') {
            config.plugins.push(new HtmlWebpackPlugin({
                template: 'src/index.ejs'
            }));
            config.resolve.extensions.push('.scss');

            config.module.rules.pop();
            config.module.rules.pop();

            config.module.rules.push({
                test: /\.scss$/,
                use: [{
                    loader: 'style-loader' // creates style nodes from JS strings
                }, {
                    loader: 'css-loader', // translates CSS into CommonJS
                    options: {
                        sourceMap: true
                    }
                }, {
                    loader: 'sass-loader' // compiles Sass to CSS
                }]
            });
        }

        return config;
    }
};
