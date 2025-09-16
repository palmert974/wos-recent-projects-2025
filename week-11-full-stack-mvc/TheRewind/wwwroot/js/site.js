// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Disable any form with data-disable-on-submit once submitted
window.addEventListener('DOMContentLoaded', () => {
  document.querySelectorAll('form[data-disable-on-submit]')
    .forEach(form => {
      form.addEventListener('submit', () => {
        const btn = form.querySelector('button[type="submit"]');
        if (btn) { btn.disabled = true; btn.classList.add('disabled'); }
      });
    });
});
