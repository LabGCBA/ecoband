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

        this.ref.on('child_added', this.onItemAdded.bind(this));
    }

    get beatsPerMinute() {
        return this._beatsPerMinute;
    }

    onItemAdded(item) {
        const data = item.val();

        this._beatsPerMinute.push({
            key: item.key,
            timestamp: data.timestamp,
            type: data.type,
            value: data.value
        });

        console.log(this._beatsPerMinute);
    }
}
