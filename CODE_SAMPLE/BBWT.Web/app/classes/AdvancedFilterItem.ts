/// <reference path="../references.ts" />
module Classes {
    export class AdvancedFilterItem {
        //fields
        title: string;
        field: string;
        operator: string;
        type: string;
        t_options: any;
        value: any;
        //

        //Builder interface
        setTitle: (title: string) => AdvancedFilterItem;
        setField: (field: string) => AdvancedFilterItem;
        setOperator: (operator: string) => AdvancedFilterItem;
        setType: (type: string) => AdvancedFilterItem;
        setOptions: (options: any) => AdvancedFilterItem;
        setValue: (value: any) => AdvancedFilterItem;
        //
        
        //ctor
        constructor() {
            //builder
            this.setTitle = (title: string) => {
                this.title = title;
                return this;
            }

            this.setField = (field: string) => {
                this.field = field;
                return this;
            }

            this.setOperator = (operator: string) => {
                this.operator = operator;
                return this;
            }

            this.setType = (type: string) => {
                this.type = type;
                return this;
            }

            this.setOptions = (options: any) => {
                this.t_options = options;
                return this;
            }

            this.setValue = (value: any) => {
                this.value = value;
                return this;
            }
        }
    }
}