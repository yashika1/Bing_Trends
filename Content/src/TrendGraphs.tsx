import * as React from "react";
import { ComboBox, IComboBoxOption } from 'office-ui-fabric-react';
import { initializeIcons } from 'office-ui-fabric-react/lib/Icons';

const graphOptions: IComboBoxOption[] = [
    { key: 'A', text: 'A' },
    { key: 'B', text: 'B' },
    { key: 'C', text: 'C' },
    { key: 'D', text: 'D' },
    { key: 'E', text: 'E' },
];
class TrendGraphs extends React.Component<any, any> {
    constructor(props) {
        super(props);        
  //      initializeIcons(undefined, { disableWarnings: true }); 
    }

    
    render() {        
        return (
             <ComboBox
                label="Select Rule Name"
                placeholder="Rule Name"
                allowFreeform
                autoComplete="on"
                options={graphOptions}
               // style={{ marginRight: '20px', width: '500px' }}
               // onChange={(event, option, index, value) => this.onRuleNameChangeForGraphView(event, option, index, value)}
            />);
    }
}

export default TrendGraphs;