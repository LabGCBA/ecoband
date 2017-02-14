import Firebase from 'firebase';

export default class Datasource {
    constructor() {
        Firebase.initializeApp({
            authDomain: 'ecoband-5e79f.firebaseapp.com',
            databaseURL: 'https://ecoband-5e79f.firebaseio.com'
        });

        this._device = 'C8:0F:10:80:DA:BE';
        this._database = Firebase.database();
        this.ref = this._database.ref(`${this._device}/activity`);
        this._beatsPerMinute = [];
        this._stepsPerMinute = [];

        this.ref.on('child_added', this.onItemAdded.bind(this));
    }

    get beatsPerMinute() {
        return this._beatsPerMinute;
    }

    onItemAdded(record) {
        const data = record.val();
        const item = {
            key: record.key,
            timestamp: data.timestamp,
            value: data.value
        };

        if (data.type === 'beatsPerMinute') this._beatsPerMinute.push(item);
        else if (data.type === 'stepsPerMinute') this._stepsPerMinute.push(item);
    }
}
