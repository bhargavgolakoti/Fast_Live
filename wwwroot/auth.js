document.querySelectorAll('.password-toggle').forEach(button => {
    button.addEventListener('click', () => {
        const input = button.closest('.input-wrap').querySelector('input');
        const visible = input.type === 'text';
        input.type = visible ? 'password' : 'text';
        button.setAttribute('aria-label', visible ? 'Show password' : 'Hide password');
        button.innerHTML = `<i class="bi bi-eye${visible ? '' : '-slash'}"></i>`;
        input.focus();
    });
});
document.querySelectorAll('form').forEach(form => form.addEventListener('submit', event => {
    if (!form.checkValidity()) return;
    const button = form.querySelector('button[type="submit"]');
    if (!button) return;
    button.disabled = true;
    button.classList.add('is-loading');
}));
