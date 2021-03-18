import axios from 'axios';

export function getViewData(symbol) {
    return axios.get(`/workerData?symbol=${symbol}`)
      .then(result => {
          return result.data;
      })
      .catch(err => { throw err });
}