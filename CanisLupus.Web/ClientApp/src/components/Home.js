import React, { Component } from 'react';
import axios from 'axios';
import { withRouter } from 'react-router-dom';
import {
  LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ScatterChart, ZAxis, Scatter, BarChart, Bar, Cell, ComposedChart
} from 'recharts';
import ViewControls from './ViewControls'



const workerData = [
  {
    time: 1,
    top: 0.0456,
    bottom: 0.0401
  },
  {
    time: 1,
    top: 0.0456,
    bottom: 0.0401
  },
  {
    time: 1,
    top: 0.0456,
    bottom: 0.0401
  },
  {
    time: 1,
    top: 0.0456,
    bottom: 0.0401
  },
  {
    time: 1,
    top: 0.0456,
    bottom: 0.0401
  },
  {
    time: 1,
    top: 0.0456,
    bottom: 0.0401
  },
];

const data01 = [{ x: 1, y: 0.046 }, { x: 4, y: 0.046 }, { x: 6, y: 0.046 }];
const data02 = [{ x: 1, y: 0.045 }, { x: 2, y: 0.045 }, { x: 5, y: 0.045 }];

const candleData = [{time: 1, value: [0.045, 0.056], fill: 'red', wma: 0.47, smma: 0.44}, 
{time: 2, value: [0.042, 0.074], fill: 'green', wma: 0.47, smma: 0.44}, 
{time: 3, value: [0.045, 0.020], fill: 'red', wma: 0.47, smma: 0.44}]

const CustomizedDot = (props) => {
  const {
    cx, cy, stroke, payload, value,
  } = props;

  if (value > 2500) {
    return (
      <svg x={cx - 10} y={cy - 10} width={20} height={20} fill="red" viewBox="0 0 1024 1024">
        <path d="M512 1009.984c-274.912 0-497.76-222.848-497.76-497.76s222.848-497.76 497.76-497.76c274.912 0 497.76 222.848 497.76 497.76s-222.848 497.76-497.76 497.76zM340.768 295.936c-39.488 0-71.52 32.8-71.52 73.248s32.032 73.248 71.52 73.248c39.488 0 71.52-32.8 71.52-73.248s-32.032-73.248-71.52-73.248zM686.176 296.704c-39.488 0-71.52 32.8-71.52 73.248s32.032 73.248 71.52 73.248c39.488 0 71.52-32.8 71.52-73.248s-32.032-73.248-71.52-73.248zM772.928 555.392c-18.752-8.864-40.928-0.576-49.632 18.528-40.224 88.576-120.256 143.552-208.832 143.552-85.952 0-164.864-52.64-205.952-137.376-9.184-18.912-31.648-26.592-50.08-17.28-18.464 9.408-21.216 21.472-15.936 32.64 52.8 111.424 155.232 186.784 269.76 186.784 117.984 0 217.12-70.944 269.76-186.784 8.672-19.136 9.568-31.2-9.12-40.096z" />
      </svg>
    );
  }

  return (
    <svg x={cx - 10} y={cy - 10} width={20} height={20} fill="green" viewBox="0 0 1024 1024">
      <path d="M517.12 53.248q95.232 0 179.2 36.352t145.92 98.304 98.304 145.92 36.352 179.2-36.352 179.2-98.304 145.92-145.92 98.304-179.2 36.352-179.2-36.352-145.92-98.304-98.304-145.92-36.352-179.2 36.352-179.2 98.304-145.92 145.92-98.304 179.2-36.352zM663.552 261.12q-15.36 0-28.16 6.656t-23.04 18.432-15.872 27.648-5.632 33.28q0 35.84 21.504 61.44t51.2 25.6 51.2-25.6 21.504-61.44q0-17.408-5.632-33.28t-15.872-27.648-23.04-18.432-28.16-6.656zM373.76 261.12q-29.696 0-50.688 25.088t-20.992 60.928 20.992 61.44 50.688 25.6 50.176-25.6 20.48-61.44-20.48-60.928-50.176-25.088zM520.192 602.112q-51.2 0-97.28 9.728t-82.944 27.648-62.464 41.472-35.84 51.2q-1.024 1.024-1.024 2.048-1.024 3.072-1.024 8.704t2.56 11.776 7.168 11.264 12.8 6.144q25.6-27.648 62.464-50.176 31.744-19.456 79.36-35.328t114.176-15.872q67.584 0 116.736 15.872t81.92 35.328q37.888 22.528 63.488 50.176 17.408-5.12 19.968-18.944t0.512-18.944-3.072-7.168-1.024-3.072q-26.624-55.296-100.352-88.576t-176.128-33.28z" />
    </svg>
  );
};


export class Home extends Component {
  constructor(props) {
    super(props);
    this.state = {
      lastMessage: 'null',
      lineData: workerData,
      candleData: candleData,
      highClusterData: data01,
      lowClusterData: data02,
      minChart: 0,
      maxChart: 0.46,
      chartViewSwitch: false
    };

    this.getWorkerData = this.getWorkerData.bind(this)
    this.onChangeChartView = this.onChangeChartView.bind(this)
  }
  static displayName = Home.name;

  componentDidMount() {
    this._loadAsyncData()
  };

  async _loadAsyncData() {
    //await this.getUserByToken()

    setInterval(async () => {
      // Your custom logic here
      await this.getWorkerData()
    }, 5000);

  }

  getWorkerData() {
    axios.get(`/workerData`)
      .then(result => {
        if (result.data !== null && result.data.candleData !== null && result.data.lowClusterData !== null && result.data.highClusterData !== null) {
          console.log(result.data)

          let lineData = result.data.candleData.map((x, i) => {
            return {
              time: i,
              top: x.top,
              bottom: x.bottom
            }
          })

          let candleData = result.data.candleData.map((x, i) => {
            return {
              time: i,
              value: [x.bottom.toFixed(5), x.top.toFixed(5)],
              fill: x.orientation < 0 ? 'red' : 'green',
              wma: x.wma.toFixed(5),
              smma: x.smma.toFixed(5)
            }
          })

          let highClusterData = result.data.highClusterData.map(x => {
            return {
              x: x.x,
              y: x.y
            }
          })

          let lowClusterData = result.data.lowClusterData.map(x => {
            return {
              x: x.x,
              y: x.y
            }
          })

          var minChart = Math.min(result.data.candleData.map(x => x.bottom))
          var maxChart = Math.max(result.data.candleData.map(x => x.top))

          this.setState({
            lineData: lineData,
            candleData: candleData,
            minChart: minChart,
            maxChart: maxChart,
            highClusterData: highClusterData,
            lowClusterData: lowClusterData
          })
        }
      }).catch(err => console.log(err))
  }

  onChangeChartView(enabled) {
    this.setState({
      chartViewSwitch: enabled
    });
  }

  renderViewControls() {
    return (
      <div>
        <ViewControls viewSwitch={this.state.chartViewSwitch}
          onChangeChartView={this.onChangeChartView} />
      </div>
    );
  }
  renderCandlesChart() {
    return (
      <div>
        <ComposedChart width={780} height={300} data={this.state.candleData}>
          <CartesianGrid strokeDasharray="3 3" />
          <XAxis dataKey="time" />
          <YAxis domain={[this.state.minChart, this.state.maxChart]} />
          <Tooltip />
          <Legend />
          <Bar dataKey="value"/>
          {
            this.state.candleData.map((entry, index) =>(
              <Cell key={`cell-${index}`} stroke={entry.fill}  strokeWidth={2}/>
            ))
          }
          <Line type="monotone" dataKey="wma" stroke="#ff7300" dot={false}/>
          <Line type="monotone" dataKey="smma" stroke="#00008b" dot={false}/>
        </ComposedChart>
      </div>
    )
  }

  renderLineChart() {
    return (
      <div>
        <LineChart
          width={780}
          height={300}
          data={this.state.lineData}
          margin={{
            top: 5, right: 30, left: 20, bottom: 5,
          }}
          baseValue={{ dataMin: 0.04, dataMax: 0.05 }}
        >
          <CartesianGrid strokeDasharray="3 3" />
          <XAxis dataKey="name" />
          <YAxis domain={[this.state.minChart, this.state.maxChart]} />
          <Tooltip />
          <Legend />
          <Line type="monotone" dataKey="top" stroke="#8884d8" />
          <Line type="monotone" dataKey="bottom" stroke="#82ca9d" />
        </LineChart>
      </div>
    )
  }


  render() {
    return (
      <div>
        <div>
          {this.renderViewControls()}
        </div>
        <div>
          {this.state.chartViewSwitch && this.renderLineChart()}
          {!this.state.chartViewSwitch && this.renderCandlesChart()}
        </div>
        <div>
          <ScatterChart
            width={780}
            height={400}
            margin={{
              top: 20, right: 40, bottom: 20, left: 40,
            }}
          >
            <CartesianGrid strokeDasharray="3 3" />
            <XAxis type="number" dataKey="x" name="time" unit="min" />
            <YAxis type="number" dataKey="y" name="value" domain={[this.state.minChart, this.state.maxChart]} />
            <ZAxis type="number" range={[100]} />
            <Tooltip cursor={{ strokeDasharray: '3 3' }} />
            <Legend />
            <Scatter name="High" data={this.state.highClusterData} fill="#8884d8" line shape="cross" />
            <Scatter name="Low" data={this.state.lowClusterData} fill="#82ca9d" line shape="diamond" />
          </ScatterChart>
        </div>
      </div>

    );
  }
}

export default withRouter(Home);
