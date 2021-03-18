import axios from 'axios'

export function insertTradingSettings(symbol) {
    return axios.post('/symbols', {symbol: symbol})
        .then(result => { return result.data })
        .catch(err => { throw err })
}

export function getAllTradingSettings() {
    return axios.get('/symbols/all')
        .then(result => { return result.data })
        .catch(err => { throw err })
}

export function getTradingSettings(symbol) {
    return axios.get(`/symbols?symbol=${symbol}`)
    .then(result => { return result.data })
    .catch(err => { throw err })
}

export function deleteTradingSettings(symbol) {
    return axios.delete(`/symbols?symbol=${symbol}`)
    .then(result => { return result.data })
    .catch(err => { throw err })
}

export function updateTradingSettings(settings) {
    return axios.put(`/symbols`, settings)
    .then(result => { return result.data })
    .catch(err => { throw err })
}