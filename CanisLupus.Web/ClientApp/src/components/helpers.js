import React from 'react';
import Switch from "react-switch";

export const createSwitch = (label, checked, onChange, id) => {
    return (
        <div className="column">
            <h3>{label}</h3>
            <Switch
                checked={checked}
                onChange={onChange}
                onColor="#86d3ff"
                onHandleColor="#2693e6"
                handleDiameter={30}
                uncheckedIcon={false}
                checkedIcon={false}
                boxShadow="0px 1px 5px rgba(0, 0, 0, 0.6)"
                activeBoxShadow="0px 0px 1px 10px rgba(0, 0, 0, 0.2)"
                height={20}
                width={48}
                className="react-switch"
                id="material-switch"
                id={id}
            />
        </div>
    );
}