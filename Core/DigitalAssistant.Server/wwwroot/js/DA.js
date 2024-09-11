window.DA = {
    Audio: null,
    OpenLinkInNewTab: function (url) {
        window.open(url, '_blank').focus();
    },
    SubmitForm: function (id) {
        document.getElementById(id).submit();
    },
    GetUserPrefersDarkMode: function () {
        if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches)
            return true;

        return false;
    },
    ScrollElementToBottom: function (id) {
        let element = document.getElementById(id);
        if (element)
            element.scrollTo(0, element.scrollHeight);
    },
    PlayAudioFileStreamAsync: async function (contentStreamReference) {
        const arrayBuffer = await contentStreamReference.arrayBuffer();
        const blob = new Blob([arrayBuffer]);
        const url = URL.createObjectURL(blob);

        var sound = document.createElement('audio');
        sound.src = url;
        sound.type = 'audio/mpeg';
        document.body.appendChild(sound);
        sound.load();
        sound.play();
        sound.onended = function () {
            document.body.removeChild(sound);
            URL.revokeObjectURL(url);
        };
    },
    PlayAudioFromUrl: function (url) {
        if(this.Audio)
            this.Audio.pause();

        this.Audio = new Audio(url);
        this.Audio.play();
    }
};