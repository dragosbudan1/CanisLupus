import React from 'react';

const LogsDisplay = ({ logs }) => {
    const renderLogs = () => {
        let latestLogs = logs
        if(logs.length > 10)
            latestLogs = logs.slice(logs.length - 10, logs.length - 1)
        return latestLogs.map(x => (
                <div>
                    {x}
                </div>
            )
        )
    }

    return (
        <div>
            {renderLogs()}
        </div>

    );
};

export default LogsDisplay;