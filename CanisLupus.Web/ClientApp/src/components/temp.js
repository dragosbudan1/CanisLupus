import React, { Component } from 'react';
import axios from 'axios';
import PumpControls from './ViewControls'
import SensorChart from './SensorChart'
import WateringSettings from './WateringSettings'
import Cookies from 'universal-cookie';
import {withRouter} from 'react-router-dom';
import {createSwitch} from './helpers'


const createSensorDataset = (colour, index) => {
  return {
    label: `Sensor ${index}`,
    fill: false,
    lineTension: 0.1,
    backgroundColor: colour,
    borderColor: colour,
    borderCapStyle: 'butt',
    borderDash: [],
    borderDashOffset: 0.0,
    borderJoinStyle: 'miter',
    pointBorderColor: colour,
    pointBackgroundColor: '#fff',
    pointBorderWidth: 1,
    pointHoverRadius: 5,
    pointHoverBackgroundColor: colour,
    pointHoverBorderColor: 'rgba(220,220,220,1)',
    pointHoverBorderWidth: 2,
    pointRadius: 1,
    pointHitRadius: 10,
    data: []
  }
}

const colours = [
  { label: "red",       index: 0, value: 'rgba(192,80,80,1)'},
  { label: "blue",      index: 1, value: 'rgba(75,75,192,1)'},
  { label: "turqoise",  index: 2, value: 'rgba(75,192,192,1)'},
  { label: "yellow",    index: 3, value: 'rgba(255, 255,0,1)'},
];

const initSensorChartData = () => {
  const datasets = []
  for(let i = 0; i < 4; i++) {
    datasets.push(createSensorDataset(colours[i].value, i))
  }
  return {
    labels: [],
    loaded: false,
    datasets: datasets
  }
}

const initPumpData = () => {
  return [
    { id: 0, active: false },
    { id: 1, active: false }, 
    { id: 2, active: false },
    { id: 3, active: false }
  ]
}

const initWateringSettings = () => {
  return [
    { id: 0, minMoisture: 0.0 },
    { id: 1, minMoisture: 2.0 }, 
    { id: 2, minMoisture: 3.0 },
    { id: 3, minMoisture: 2.0 }
  ]
}

const options = [
  { value: (1 + 4 * 1), label: '1 hour' },
  { value: (1 + 4 * 6), label: '6 hours' },
  { value: 1 + 4 * 12, label: '12 hours' },
  { value: 1 + 4 * 24, label: '24 hours' },
  { value: 1 + 4 * 48, label: '48 hours' },
  { value: 1 + 4 * 24 * 3, label: '3 days' },
  { value: 1 + 4 * 24 * 7, label: '7 days' }

];
const defaultOption = options[0];

export class Home extends Component {
  constructor(props) {
    super(props);
    this.state = {
      pumpsData: initPumpData(),
      sensorChartData: initSensorChartData(),
      rangeDropdownValue: defaultOption,
      autoPump: false,
      authenticated: true,
      controlsSwitch: false,
      wateringSettings: initWateringSettings()
    };

    this.getSensorData = this.getSensorData.bind(this)
    this.onSensorDataRangeSelect = this.onSensorDataRangeSelect.bind(this)
    this.setAutoPump = this.setAutoPump.bind(this)
    this.setPump = this.setPump.bind(this)
    this.getUserByToken = this.getUserByToken.bind(this)
    this.onControlsSwitch = this.onControlsSwitch.bind(this)
    this.getWateringSettings = this.getWateringSettings.bind(this)
    this.setWateringSettings = this.setWateringSettings.bind(this)
    this.handleWateringSettingsChange = this.handleWateringSettingsChange.bind(this)
  }
  static displayName = Home.name;

  componentDidMount() {
    this._loadAsyncData()
  };

  async _loadAsyncData() {
      //await this.getUserByToken()
      await this.getSensorData(20)
      await this.getLatestAutoPumpData()
      await this.getWateringSettings()
  }

  renderShowSettings() {
    return (
        <div>
            <div className="row">
               {createSwitch("Show Settings", this.state.controlsSwitch, this.onControlsSwitch)}
            </div>
        </div>
    );
}

  renderSensorChart() {
    return (
      <div>
        {this.state.sensorChartData.loaded && <SensorChart sensorChartData={this.state.sensorChartData} 
          onSensorDataRangeSelect = {this.onSensorDataRangeSelect} 
          dropdownValue={this.state.rangeDropdownValue}
          />
        }
      </div>
    );
  }

  renderPumpControls() {
    return (
      <div>
        {this.state.pumpsData &&  <PumpControls pumpsData={this.state.pumpsData} 
          onSetPump={this.setPump} 
          autoPump={this.state.autoPump} 
          onSetAutoPump={this.setAutoPump} />}
      </div>
    );
  }

  renderWateringSettings() {
    return (
      <div>
        {this.state.wateringSettings && <WateringSettings 
          setWateringSettings={this.setWateringSettings} 
          handleWateringSettingsChange={this.handleWateringSettingsChange} 
          wateringSettings={this.state.wateringSettings}/>}
      </div>
    )
  }

  renderAll() {
    return (
      <div>
        <h1>Dashboard</h1>
        {this.renderSensorChart()}
        <br></br>
        {this.renderShowSettings()}
        <br></br>
        {this.state.controlsSwitch && this.renderPumpControls()}
        <br></br>
        {this.state.controlsSwitch && this.renderWateringSettings()}      
      </div>    
    );
  }

  render () {
    return (
      <div>
        {this.state.authenticated && this.renderAll()}
      </div>    
    );
  }

  onControlsSwitch(enabled) {
    this.setState({
      controlsSwitch: enabled
    })
  }

  onSensorDataRangeSelect(val) {
    this.setState({
      rangeDropdownValue: val
    })
    this.getSensorData(val.value * 4)
  }

  getSensorData(count) {
    axios.get(`/sensorData?count=${count}`)
      .then(result => {
        this.mapSensorData(result.data)
      }).catch(err => console.log(err))
  }

  getLatestAutoPumpData() {
    axios.get("/autoPump")
      .then(result => {
        this.setState({
          autoPump: result.data.enabled
        })
      })
  }

  handleWateringSettingsChange(event) {
    const {wateringSettings} = this.state
    wateringSettings[event.target.id].minMoisture = event.target.value
    this.setState({
      wateringSettings: wateringSettings
    })

  }

  setWateringSettings(event) {
    event.preventDefault();
    console.log(this.state.wateringSettings)
    axios.post('/wateringSettings', this.state.wateringSettings.map(x => {
      return {
        id: x.id,
        minMoisture: parseFloat(x.minMoisture)
      }
    }))
      .then(result => {
        console.log(result)
      })
      .catch(err => console.log(err))
  }

  setAutoPump(enabled) {
    axios.post("/autoPump", {
      enabled: enabled
    }).then(result => {
      let autoPump = result.data
      this.setState({
        autoPump: autoPump.enabled
      })
    }).catch(err => console.log(err))
  }

  setPump(active, event, id) {
    const { pumpsData } = this.state
    axios.post("/pump", { 
      id: id,
      active: active
    }).then(result => {}).catch(err => console.log(err))

    const updatedPumps = pumpsData.map(pump => {
      if(pump.id === id) {
        pump.active = active
      }
      return pump
    })
    this.setState({
      pumpsData: updatedPumps
    });
  }

  mapSensorData(data) {
    const { sensorChartData } = this.state
    let sensor2Data = data.filter(x => x.id == 2)
    let labels = []
    sensor2Data.forEach(x => labels.push(x.date))
    let sensorData = []
    sensor2Data.forEach(x => sensorData.push(x.moisture))

    sensorChartData.labels = labels
    sensorChartData.datasets[0].data = data.filter(x => x.id === 0).map(x => x.moisture)
    sensorChartData.datasets[1].data = data.filter(x => x.id === 1).map(x => x.moisture)
    sensorChartData.datasets[2].data = data.filter(x => x.id === 2).map(x => x.moisture)
    sensorChartData.datasets[3].data = data.filter(x => x.id === 3).map(x => x.moisture)

    sensorChartData.loaded = true

    this.setState({
      sensorChartData: sensorChartData
    });

  }

  getWateringSettings() {
    axios.get('/wateringSettings')
      .then(result => {
        if(result.data != null && result.data.length == 4)
        this.setState({
          wateringSettings: result.data.map(x => {
            return {
              id: x.id,
              minMoisture: x.minMoisture
            }
          })
        })
      })
  }

  getUserByToken() {
    const cookies = new Cookies();
    const token = cookies.get('_authToken');
    if(token !== undefined && token !== "") {
      axios.get(`/authenticate?token=${token}`).then(result => {
        if(result.data.success === true && 
          result.data.token === token) {
            this.setState({
              authenticated: true
            });
        } else {
          this.props.history.push('/auth');
        }
      }).catch(err => console.log(err))
    } else {
      this.props.history.push('/auth');
    }
  }

  getWateringSettings() {
    axios.get('/wateringSettings').then(result => {
      if(result.data !== null && result.data.length != 0) {      
        this.setState({
          wateringSettings: result.data
        })
      }
    })
  }
}

export default withRouter(Home);
