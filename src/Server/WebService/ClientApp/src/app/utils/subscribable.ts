
import { Observable, ReplaySubject, Subject } from "rxjs";

export class Subscribable<T> {

    private valueSource: Subject<T> = new ReplaySubject<T>(1);
    public value: Observable<T>;
    private _value: T;

    constructor() {
        this.value = this.valueSource.asObservable();
    }

    public set(val: T) {
        this.valueSource.next(val);
        this._value = val;
    }

    public get(): T {
        return this._value;
    }

    public notify() {
        this.valueSource.next(this._value);
    }

    public error(message: any) {
        this.valueSource.error(message);
    }
}
