// Button Loading State
function setButtonLoading(btn, isLoading) {
    if (isLoading) {
        btn.dataset.originalText = btn.innerHTML;
        btn.disabled = true;
        btn.innerHTML = '<span class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>Processing...';
    } else {
        btn.disabled = false;
        btn.innerHTML = btn.dataset.originalText;
    }
}

// Global Toast System
// Global Toast System using SweetAlert2
const Toast = Swal.mixin({
    toast: true,
    position: 'top-end',
    showConfirmButton: false,
    timer: 3000,
    timerProgressBar: true,
    didOpen: (toast) => {
        toast.addEventListener('mouseenter', Swal.stopTimer)
        toast.addEventListener('mouseleave', Swal.resumeTimer)
    }
});

function showToast(message, type = 'info') {
    // Map bootstrap types to sweetalert icons
    let icon = 'info';
    if (type === 'success') icon = 'success';
    if (type === 'error' || type === 'danger') icon = 'error';
    if (type === 'warning') icon = 'warning';

    Toast.fire({
        icon: icon,
        title: message
    });
}

// Global Loading Spinner
function showLoading(text = 'Loading...') {
    const overlay = document.getElementById('loadingOverlay');
    const loadingText = overlay.querySelector('.loading-text');
    if (loadingText) loadingText.textContent = text;
    if (overlay) overlay.style.display = 'flex';
}

function hideLoading() {
    const overlay = document.getElementById('loadingOverlay');
    if (overlay) overlay.style.display = 'none';
}

function toggleTheme() {
    const html = document.documentElement;
    const current = html.getAttribute('data-bs-theme');
    const next = current === 'dark' ? 'light' : 'dark';

    html.setAttribute('data-bs-theme', next);
    localStorage.setItem('theme', next);

    const icon = next === 'dark' ? 'fa-sun' : 'fa-moon';
    const btn = document.getElementById('themeToggle');
    if (btn) btn.innerHTML = `<i class="fas ${icon}"></i>`;
}
