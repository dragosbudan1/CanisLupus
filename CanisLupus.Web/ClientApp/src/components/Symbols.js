import React, { Component } from 'react';
import { withRouter } from 'react-router-dom';
import { insertTradingSettings, getAllTradingSettings, deleteTradingSettings } from './tradingSettingsService'

export class Symbols extends Component {
    constructor(props) {
        super(props);
        this.state = {
            currentSymbol: "",
            tradingSettings: []
        }
        this.onChangeSymbol = this.onChangeSymbol.bind(this)
        this.onUpdateSymbol = this.onUpdateSymbol.bind(this)
        this.onDeleteSymbol = this.onDeleteSymbol.bind(this)
        this.getTradingSettings = this.getTradingSettings.bind(this)
    }

    componentDidMount() {
        this._loadAsyncData()
    };

    async _loadAsyncData() {
        await this.getTradingSettings()
    }

    onChangeSymbol(e) {
        this.setState({
            currentSymbol: e.target.value
        })
    }

    async onUpdateSymbol() {
        if (this.state.symbol != "") {
            var result = await insertTradingSettings(this.state.currentSymbol)
            await this.getTradingSettings()
        }
    }

    async onDeleteSymbol(e) {
        await deleteTradingSettings(e.target.value)
        await this.getTradingSettings()
    }

    async getTradingSettings() {
        var result = await getAllTradingSettings();
        console.log(result)
        if (result != null) {
            this.setState({
                tradingSettings: result
            });
        }
    }

    renderSymbolsList() {
        return (
            <div>
                {this.state.tradingSettings.map((settings, index) => {
                    return (
                        <div key={index} className="row">
                            {settings.symbol}
                            <button
                                value={settings.symbol}
                                id={`${index + 1}`}
                                onClick={this.onDeleteSymbol}>
                                    Remove
                            </button>
                        </div>
                    )
                })}
            </div>
        )
    }

    render() {
        return (
            <div>
                <div className="row">
                    <label>
                        Symbol:
              <input type="text" value={this.state.currentSymbol} onChange={this.onChangeSymbol} />
                    </label>
                    <button onClick={this.onUpdateSymbol}>
                        Update
                    </button>
                </div>
                {this.renderSymbolsList()}
            </div>

        )
    }
}

export default withRouter(Symbols)