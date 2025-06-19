const functions = require('firebase-functions');
const admin = require('firebase-admin');
admin.initializeApp();

exports.changePassword = functions.https.onCall(async (data, context) => {
    const { uid, newPassword } = data;

    if (!context.auth) {
        throw new functions.https.HttpsError('unauthenticated', 'User must be authenticated to change password.');
    }

    try {
        await admin.auth().updateUser(uid, { password: newPassword });
        return { message: 'Password changed successfully.' };
    } catch (error) {
        throw new functions.https.HttpsError('unknown', error.message, error);
    }
});
