import React from 'react';
import {createSwitch} from './helpers'

const ViewControls = ({viewSwitch, onChangeChartView}) => {

    const renderControls = () => {
        return (
        <div>
            <div className="row">
                {createSwitch("Chart View", viewSwitch, onChangeChartView, 'chartView')}
            </div>
        </div>
        );
    }

    return(
        <div>
            {renderControls()}
        </div>
        
    );
};

export default ViewControls;