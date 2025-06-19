const express = require('express');
const path = require('path');

const app = express();
const PORT = 3000;

// Serve the reset password form
app.use('/reset-password', express.static(path.join(__dirname, 'resetPasswordForm.html')));

app.listen(PORT, () => {
    console.log(`Server running on https://praise-game.eu/reset-password`);
});
