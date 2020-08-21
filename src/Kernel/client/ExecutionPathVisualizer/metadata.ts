import { DataAttributes } from './circuit';

/**
 * Enum for the various gate operations handled.
 */
export enum GateType {
    /** Measurement gate. */
    Measure,
    /** CNOT gate. */
    Cnot,
    /** SWAP gate. */
    Swap,
    /** Single/multi qubit unitary gate. */
    Unitary,
    /** Single/multi controlled unitary gate. */
    ControlledUnitary,
    /** Nested group of classically-controlled gates. */
    ClassicalControlled,
    /** Group of nested gates */
    Group,
    /** Invalid gate. */
    Invalid,
}

/**
 * Metadata used to store information pertaining to a given
 * operation for rendering its corresponding SVG.
 */
export interface Metadata {
    /** Gate type. */
    type: GateType;
    /** Centre x coord for gate position. */
    x: number;
    /** Array of y coords of control registers. */
    controlsY: number[];
    /** Array of y coords of target registers.
     *  For `GateType.Unitary` or `GateType.ControlledUnitary`, this is an array of groups of
     *  y coords, where each group represents a unitary box to be rendered separately.
     */
    targetsY: (number | number[])[];
    /** Gate label. */
    label: string;
    /** Gate arguments as string. */
    displayArgs?: string;
    /** Gate width. */
    width: number;
    /** Children operations as part of group. */
    children?: Metadata[];
    /** Classically-controlled gates.
     *  - conditionalChildren[0]: metadata of gates rendered when classical control bit is 0.
     *  - conditionalChildren[1]: metadata of gates rendered when classical control bit is 1.
     */
    conditionalChildren?: [Metadata[], Metadata[]];
    /** HTML element class for interactivity. */
    htmlClass?: string;
    /** Custom data attributes to attach to gate element. */
    dataAttributes?: DataAttributes;
}
