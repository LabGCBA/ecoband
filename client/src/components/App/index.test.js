import App from './';
import React from 'react';
import { shallow } from 'enzyme';

it('Test example', () => {
  const wrapper = shallow(<App />);
  expect(wrapper.is('div')).toBeTruthy();
});
